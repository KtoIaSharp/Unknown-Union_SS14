using Content.Shared._UU.Radio;
using Robust.Client.UserInterface;

namespace Content.Client._UU.Radio;

public sealed class TransmitterBui : BoundUserInterface
{
    private TransmitterWindow? _window;

    public TransmitterBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new TransmitterWindow();
        _window.OnFrequencySubmitted += (freq, name) =>
        {
            SendMessage(new FrequencySetMessage(freq, name));
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
