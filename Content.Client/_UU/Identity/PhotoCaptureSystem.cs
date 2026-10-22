using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Content.Shared._UU.Identity;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._UU.Identity;

/// <summary>
///     Снимает спрайт игрока для фотографии. Пиксели есть только у клиента,
///     поэтому сервер присылает запрос, а мы возвращаем готовый PNG в base64.
/// </summary>
public sealed class PhotoCaptureSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private PhotoCaptureControl? _control;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PhotoCaptureRequestEvent>(OnRequest);
    }

    private async void OnRequest(PhotoCaptureRequestEvent ev)
    {
        string? image = null;

        try
        {
            image = await CaptureAsync(ev.Target);
        }
        catch (Exception)
        {
            // Не удалось — фото останется без снимка, а не уронит клиент.
        }

        RaiseNetworkEvent(new PhotoCaptureResponseEvent
        {
            Photo = ev.Photo,
            ImageBase64 = image,
        });
    }

    private async Task<string?> CaptureAsync(NetEntity target)
    {
        if (!TryGetEntity(target, out var entity) || entity is not { } uid)
            return null;

        if (!EntityManager.TryGetComponent<SpriteComponent>(uid, out _))
            return null;

        if (_control == null)
        {
            _control = new PhotoCaptureControl();
            _ui.RootControl.AddChild(_control);
        }

        return await _control.Capture(uid);
    }
}

/// <summary>
///     Служебный контрол: рисует сущность в render target и отдаёт PNG.
///     Висит в дереве UI только потому, что рисовать можно лишь из Draw.
/// </summary>
public sealed class PhotoCaptureControl : Control
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly Queue<(EntityUid Entity, TaskCompletionSource<string?> Completion)> _queue = new();

    public PhotoCaptureControl()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Ignore;
    }

    public Task<string?> Capture(EntityUid entity)
    {
        var completion = new TaskCompletionSource<string?>();
        _queue.Enqueue((entity, completion));
        return completion.Task;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        while (_queue.TryDequeue(out var job))
        {
            if (!_entityManager.EntityExists(job.Entity))
            {
                job.Completion.SetResult(null);
                continue;
            }

            var target = _clyde.CreateRenderTarget(
                new Vector2i(32, 32),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb));

            try
            {
                handle.RenderInRenderTarget(target, () =>
                {
                    handle.DrawEntity(job.Entity, new Vector2(16, 16), Vector2.One, Angle.Zero);
                }, Color.Transparent);

                target.CopyPixelsToMemory<Rgba32>(image =>
                {
                    try
                    {
                        using var stream = new MemoryStream();
                        image.SaveAsPng(stream);
                        job.Completion.SetResult(Convert.ToBase64String(stream.ToArray()));
                    }
                    catch (Exception)
                    {
                        job.Completion.SetResult(null);
                    }
                    finally
                    {
                        image.Dispose();
                    }
                });
            }
            catch (Exception)
            {
                job.Completion.SetResult(null);
            }
            finally
            {
                target.Dispose();
            }
        }
    }
}
