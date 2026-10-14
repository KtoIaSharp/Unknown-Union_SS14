using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;

namespace Content.Client._UU.EasterEgg;

public sealed class EasterEggSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private int _step;

    private static readonly PopupType[] XColors = [
        PopupType.Small,
        PopupType.MediumCaution,
        PopupType.Medium,
        PopupType.LargeCaution,
        PopupType.Large,
        PopupType.SmallCaution,
    ];

    public override void Initialize()
    {
        base.Initialize();
        _input.FirstChanceOnKeyEvent += OnKeyEvent;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.FirstChanceOnKeyEvent -= OnKeyEvent;
    }

    private void OnKeyEvent(KeyEventArgs args, KeyEventType type)
    {
        if (type != KeyEventType.Down || !args.Control)
        {
            if (type == KeyEventType.Up && !args.Control)
                _step = 0;
            return;
        }

        if (args.Key == Keyboard.Key.Z && _step == 0)
            _step = 1;
        else if (args.Key == Keyboard.Key.X && _step == 1)
            _step = 2;
        else if (args.Key == Keyboard.Key.C && _step == 2)
        {
            _step = 0;
            Trigger();
        }
        else
            _step = 0;
    }

    private void Trigger()
    {
        var ent = _player.LocalEntity;
        if (ent == null)
            return;

        EntityManager.RaisePredictiveEvent(new PlayEmoteMessage("Dance"));

        for (var i = 0; i < 6; i++)
        {
            _popup.PopupEntity("X", ent.Value, XColors[i]);
        }
    }
}
