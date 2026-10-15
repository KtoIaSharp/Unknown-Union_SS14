using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
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
/// В отличие от отдельного парсера XAML этот путь даёт точное совпадение с игрой,
/// потому что использует тот же UI-стек движка. Цена — нужно запустить клиент.
/// </summary>
public sealed partial class UiPreviewGenerator
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IResourceManager _resource = default!;

    private ISawmill _sawmill = default!;

    private bool _requested;
    private bool _running;
    private int _warmupLeft;

    private readonly List<Control> _opened = new();

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("uu.ui-preview");
    }

    /// <summary>
    /// Возвращает true, если включён batch-режим: обычный запуск клиента прерывается,
    /// как это делает генератор скриншотов сущностей.
    /// </summary>
    public bool PostInit()
    {
        if (!_cfg.GetCVar(CCVars.UiPreviewEnabled))
            return false;

        _requested = true;
        _warmupLeft = Math.Max(1, _cfg.GetCVar(CCVars.UiPreviewWarmupFrames));
        return true;
    }

    public void Update()
    {
        if (!_requested || _running)
            return;

        if (_warmupLeft-- > 0)
            return;

        _running = true;
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        var windowNames = _cfg.GetCVar(CCVars.UiPreviewWindows)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (windowNames.Length == 0)
        {
            _sawmill.Error("uu.ui_preview.windows пуст — нечего рисовать.");
            return;
        }

        foreach (var name in windowNames)
        {
            try
            {
                await RenderWindow(name);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Не удалось отрисовать {name}: {e}");
            }
        }

        _sawmill.Info("Отрисовка интерфейса завершена.");
    }

    private async Task RenderWindow(string name)
    {
        var type = ResolveType(name);
        if (type == null)
        {
            _sawmill.Error($"Тип окна не найден: {name}");
            return;
        }

        if (Activator.CreateInstance(type) is not Control control)
        {
            _sawmill.Error($"Тип {type.FullName} не является Control.");
            return;
        }

        _ui.RootControl.AddChild(control);
        _opened.Add(control);

        // Даём UI-стеку разложить окно и подтянуть стили/шрифты/спрайты.
        for (var i = 0; i < 3; i++)
            await Task.Yield();

        var position = control.GlobalPosition;
        var size = control.Size;

        if (size.X < 1f || size.Y < 1f)
        {
            _sawmill.Warning($"{type.Name}: раскладка дала нулевой размер, кадр пропущен.");
            control.Orphan();
            _opened.Remove(control);
            return;
        }

        // subRegion не используем: снимаем кадр целиком и обрезаем сами,
        // чтобы не зависеть от семантики UIBox2i.
        using var frame = await _clyde.ScreenshotAsync(ScreenshotType.Final);

        var rect = ClampRect(position, size, frame.Width, frame.Height);
        if (rect.Width < 1 || rect.Height < 1)
        {
            _sawmill.Warning($"{type.Name}: окно вне кадра ({position} {size}).");
            control.Orphan();
            _opened.Remove(control);
            return;
        }

        frame.Mutate(x => x.Crop(rect));

        var dir = new ResPath(_cfg.GetCVar(CCVars.UiPreviewOutputPath));
        var path = dir / $"{type.Name}.png";
        _resource.UserData.CreateDir(dir);

        await using (var stream = _resource.UserData.OpenWrite(path))
        {
            await frame.SaveAsPngAsync(stream);
        }

        _sawmill.Info($"Записано {path} ({rect.Width}x{rect.Height}).");

        control.Orphan();
        _opened.Remove(control);
    }

    private static Rectangle ClampRect(Vector2 position, Vector2 size, int frameWidth, int frameHeight)
    {
        var left = Math.Clamp((int) MathF.Floor(position.X), 0, Math.Max(0, frameWidth - 1));
        var top = Math.Clamp((int) MathF.Floor(position.Y), 0, Math.Max(0, frameHeight - 1));
        var right = Math.Clamp((int) MathF.Ceiling(position.X + size.X), left + 1, frameWidth);
        var bottom = Math.Clamp((int) MathF.Ceiling(position.Y + size.Y), top + 1, frameHeight);
        return new Rectangle(left, top, right - left, bottom - top);
    }

    private Type? ResolveType(string name)
    {
        var controls = _reflection.GetAllChildren<Control>().ToList();

        return controls.FirstOrDefault(t => t.FullName == name)
               ?? controls.FirstOrDefault(t => t.Name == name);
    }
}
