using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Client.Gameplay;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Client._UU.UiPreview;

/// <summary>
/// Batch-режим: рисует указанные XAML-окна клиента в PNG «как в игре» —
/// с настоящим stylesheet, шрифтами, RSI-спрайтами и локализацией.
///
/// Запуск:
///   Content.Client.exe --cvar uu.ui_preview.enabled=true
///                      --cvar uu.ui_preview.windows=Content.Client._UU.Radio.TransmitterWindow
///                      --cvar uu.ui_preview.output_path=/UiPreview
///
/// Ограничения песочницы контента, из-за которых код написан именно так:
/// запрещены Activator.CreateInstance (используется IDynamicTypeFactory),
/// Task.Yield (кадры ждём через TaskCompletionSource из Update)
/// и ImageSharp Mutate (кадр обрезается subRegion'ом самого движка).
/// </summary>
public sealed partial class UiPreviewGenerator
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IResourceManager _resource = default!;
    [Dependency] private IDynamicTypeFactory _factory = default!;
    [Dependency] private IGameController _gameController = default!;
    [Dependency] private IBaseClient _baseClient = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private ISawmill _sawmill = default!;

    /// <summary>Сколько кадров максимум ждать подъёма UI, прежде чем сдаться.</summary>
    private const int MaxWaitFrames = 1200;

    private readonly List<string> _queue = new();
    private int _queueIndex;
    private bool _started;
    private bool _requested;
    private bool _startupRequested;
    private int _waitedFrames;

    private TaskCompletionSource? _framesReady;
    private long _waitUntilFrame;
    private Control? _current;
    private Control? _host;

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("uu.ui-preview");
    }

    /// <summary>
    /// Возвращает true, если включён batch-режим: обычный старт не продолжаем.
    /// Поднимаем одиночную игру — иначе системы сущностей не инициализированы
    /// и EntityPrototypeView падает в NRE на GetEntitySystem.
    /// </summary>
    public bool PostInit()
    {
        if (!_cfg.GetCVar(CCVars.UiPreviewEnabled))
            return false;

        _requested = true;
        _queue.AddRange(_cfg.GetCVar(CCVars.UiPreviewWindows)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (_baseClient.RunLevel == ClientRunLevel.SinglePlayerGame)
            return true;

        if (!_startupRequested)
        {
            _startupRequested = true;
            _baseClient.StartSinglePlayer();
            _stateManager.RequestStateChange<GameplayState>();
        }

        return true;
    }

    public void Update()
    {
        if (!_requested)
            return;

        // Ждём, пока поднимется игровая сессия: вместе с ней инициализируются
        // системы сущностей, без которых не работает EntityPrototypeView.
        if (_baseClient.RunLevel != ClientRunLevel.SinglePlayerGame)
        {
            _waitedFrames++;

            if (_waitedFrames > MaxWaitFrames)
            {
                _sawmill.Error($"Игровая сессия не поднялась за {MaxWaitFrames} кадров — отрисовка отменена.");
                _requested = false;
            }

            return;
        }

        // Ожидание кадров: раскладка, шрифты, стили, спрайты.
        // Считаем по CurFrame, а не по вызовам Update: EntryPoint.Update зовётся
        // несколько раз за кадр, на каждом ModUpdateLevel.
        if (_waitUntilFrame != 0)
        {
            if (_timing.CurFrame >= _waitUntilFrame)
            {
                _waitUntilFrame = 0;
                _framesReady?.TrySetResult();
            }

            return;
        }

        if (_started)
            return;

        _started = true;

        if (_queue.Count == 0)
        {
            _sawmill.Error("uu.ui_preview.windows пуст — нечего рисовать.");
            return;
        }

        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        while (_queueIndex < _queue.Count)
        {
            var name = _queue[_queueIndex++];

            try
            {
                await RenderWindow(name);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Не удалось отрисовать {name}: {e}");
                Cleanup();
            }
        }

        _sawmill.Info("Отрисовка интерфейса завершена.");

        if (_cfg.GetCVar(CCVars.UiPreviewQuitAfter))
            _gameController.Shutdown("uu.ui_preview finished");
    }

    private async Task RenderWindow(string name)
    {
        var type = ResolveType(name);
        if (type == null)
        {
            _sawmill.Error($"Тип окна не найден: {name}");
            return;
        }

        if (_factory.CreateInstance(type) is not Control control)
        {
            _sawmill.Error($"Тип {type.FullName} не является Control.");
            return;
        }

        // UIRoot растягивает детей на весь экран, поэтому кладём окно в центр-контейнер:
        // так оно сохраняет свой натуральный размер, а кадр обрезается ровно по нему.
        var host = new CenterContainer();
        _ui.RootControl.AddChild(host);
        host.AddChild(control);
        _host = host;
        _current = control;

        await WaitFrames(3);

        // Ждём именно раскладки: UI считает размеры в потоке отрисовки,
        // поэтому окно может быть ещё нулевым через пару кадров.
        for (var attempt = 0; attempt < 30 && (control.Size.X < 1f || control.Size.Y < 1f); attempt++)
            await WaitFrames(2);

        var position = control.GlobalPosition;
        var size = control.Size;

        if (size.X < 1f || size.Y < 1f)
        {
            _sawmill.Warning($"{type.Name}: раскладка дала нулевой размер, кадр пропущен.");
            Cleanup();
            return;
        }

        var box = new UIBox2i(
            (int) MathF.Floor(position.X),
            (int) MathF.Floor(position.Y),
            (int) MathF.Ceiling(position.X + size.X),
            (int) MathF.Ceiling(position.Y + size.Y));

        // Движок отдаёт только полноэкранный кадр: IClyde.ScreenshotAsync не пробрасывает
        // subRegion, а IClyde.Screenshot берёт из него лишь размер и читает с (0,0).
        // Image.Mutate в песочнице запрещён, но Clone(action) и CropExtensions разрешены.
        var full = await _clyde.ScreenshotAsync(ScreenshotType.Final);

        var width = Math.Clamp(box.Width, 1, full.Width);
        var height = Math.Clamp(box.Height, 1, full.Height);
        var rect = new Rectangle(
            Math.Clamp(box.Left, 0, full.Width - width),
            Math.Clamp(box.Top, 0, full.Height - height),
            width,
            height);

        using var image = full.Clone(ctx => ctx.Crop(rect));
        full.Dispose();

        var dir = new ResPath(_cfg.GetCVar(CCVars.UiPreviewOutputPath));
        _resource.UserData.CreateDir(dir);
        var path = dir / $"{type.Name}.png";

        await using (var stream = _resource.UserData.OpenWrite(path))
        {
            image.SaveAsPng(stream);
        }

        _sawmill.Info($"Записано {path} ({image.Width}x{image.Height}).");

        Cleanup();
    }

    /// <summary>
    /// Ждёт указанное число кадров отрисовки, не используя Task.Yield (запрещён в песочнице).
    /// </summary>
    private Task WaitFrames(int count)
    {
        _waitUntilFrame = _timing.CurFrame + Math.Max(1, count);
        _framesReady = new TaskCompletionSource();
        return _framesReady.Task;
    }

    private void Cleanup()
    {
        _current = null;

        if (_host == null)
            return;

        _host.Orphan();
        _host = null;
    }

    private Type? ResolveType(string name)
    {
        var controls = _reflection.GetAllChildren<Control>().ToList();

        return controls.FirstOrDefault(t => t.FullName == name)
               ?? controls.FirstOrDefault(t => t.Name == name);
    }
}
