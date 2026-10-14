using System.Numerics;
using Content.Shared._UU.Radio;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._UU.Radio;

public sealed class TransmitterWindow : DefaultWindow
{
    private readonly LineEdit _freqInput;
    private readonly LineEdit _nameInput;
    private readonly Button _setButton;
    private readonly Label _currentLabel;

    public event Action<string, string?>? OnFrequencySubmitted;

    public TransmitterWindow()
    {
        Title = Loc.GetString("radio-transmitter-title");
        MinSize = new Vector2(350, 170);

        var root = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 4 };
        ContentsContainer.AddChild(root);

        _currentLabel = new Label { Text = Loc.GetString("radio-no-frequency") };
        root.AddChild(_currentLabel);

        root.AddChild(new Control { MinSize = new Vector2(0, 6) });
        root.AddChild(new Label { Text = Loc.GetString("radio-enter-frequency") });

        _freqInput = new LineEdit { PlaceHolder = Loc.GetString("radio-frequency-placeholder") };
        root.AddChild(_freqInput);

        root.AddChild(new Label { Text = Loc.GetString("radio-enter-name") });

        _nameInput = new LineEdit { PlaceHolder = Loc.GetString("radio-name-placeholder") };
        root.AddChild(_nameInput);

        _setButton = new Button { Text = Loc.GetString("radio-set-frequency") };
        _setButton.OnPressed += _ =>
        {
            var freq = _freqInput.Text.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(freq))
            {
                var name = _nameInput.Text.Trim();
                OnFrequencySubmitted?.Invoke(freq, string.IsNullOrWhiteSpace(name) ? null : name);
            }
        };
        root.AddChild(_setButton);
    }

    public void UpdateState(FrequencyBuiState state)
    {
        var freqStr = state.Frequency != null
            ? Loc.GetString("radio-current-frequency", ("freq", (object)state.Frequency))
            : Loc.GetString("radio-no-frequency");
        if (state.Frequency != null && state.FrequencyName != null)
            freqStr += $" ({state.FrequencyName})";
        _currentLabel.Text = freqStr;
    }
}
