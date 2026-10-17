using Content.Shared._UU.Radio;
using Robust.Client.UserInterface;

namespace Content.Client._UU.Radio;

public sealed class TowerBui : BoundUserInterface
{
    private TowerWindow? _window;

    public TowerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new TowerWindow();
        _window.OnConfigureSubmitted += (code, freq) =>
        {
            SendMessage(new TowerConfigureMessage(code, freq));
        };
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is TowerBuiState towerState)
            _window?.UpdateState(towerState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
    }
}
