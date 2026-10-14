using Content.Shared._UU.Radio;
using Robust.Client.UserInterface;

namespace Content.Client._UU.Radio;

public sealed class JammerBui : BoundUserInterface
{
    private JammerWindow? _window;

    public JammerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new JammerWindow();
        _window.OnFrequencySet += (freq) =>
        {
            SendMessage(new FrequencySetMessage(freq));
        };
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is FrequencyBuiState freqState)
            _window?.UpdateState(freqState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
    }
}
