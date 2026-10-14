using System.Numerics;
using Content.Shared._UU.Radio;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._UU.Radio;

public sealed class JammerWindow : DefaultWindow
{
    private readonly LineEdit _freqInput;
    private readonly Button _setButton;
    private readonly Label _currentLabel;

    public event Action<string>? OnFrequencySet;

    public JammerWindow()
    {
        Title = Loc.GetString("radio-jammer-title");
        MinSize = new Vector2(300, 120);

        var root = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 4 };
        ContentsContainer.AddChild(root);

        _currentLabel = new Label { Text = Loc.GetString("radio-jammer-no-target") };
        root.AddChild(_currentLabel);

        root.AddChild(new Control { MinSize = new Vector2(0, 6) });
        root.AddChild(new Label { Text = Loc.GetString("radio-enter-frequency") });

        _freqInput = new LineEdit { PlaceHolder = Loc.GetString("radio-frequency-placeholder") };
        root.AddChild(_freqInput);

        _setButton = new Button { Text = Loc.GetString("radio-set-frequency") };
        _setButton.OnPressed += _ =>
        {
            var freq = _freqInput.Text.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(freq))
                OnFrequencySet?.Invoke(freq);
        };
        root.AddChild(_setButton);
    }

    public void UpdateState(FrequencyBuiState state)
    {
        _currentLabel.Text = state.Frequency != null
            ? Loc.GetString("radio-jammer-target", ("frequency", (object)state.Frequency))
            : Loc.GetString("radio-jammer-no-target");
    }
}
