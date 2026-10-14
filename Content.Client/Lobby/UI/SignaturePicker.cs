using Content.Client.Lobby.UI;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed class SignaturePicker : BoxContainer
{
    private readonly SpriteSystem _spriteSystem;
    private string? _selectedSignature;
    private readonly Button[] _signatureButtons = new Button[10];

    public string? SelectedSignature => _selectedSignature;

    public event Action<string?>? OnSignatureSelected;

    public SignaturePicker()
    {
        _spriteSystem = IoCManager.Resolve<IEntityManager>().System<SpriteSystem>();

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;
        Margin = new Thickness(10);

        var sigNames = new[]
        {
            "sig01", "sig02", "sig03", "sig04", "sig05",
            "sig06", "sig07", "sig08", "sig09", "sig10"
        };

        // Title
        var title = new Label
        {
            Text = Loc.GetString("signature-picker-title"),
            StyleClasses = { "LabelHeadingBigger" },
            Margin = new Thickness(0, 0, 0, 10),
        };
        AddChild(title);

        var desc = new Label
        {
            Text = Loc.GetString("signature-picker-description"),
            StyleClasses = { "LabelSubText" },
            Margin = new Thickness(0, 0, 0, 10),
        };
        AddChild(desc);

        // Container for signature buttons
        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        AddChild(scroll);

        var grid = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 10,
        };
        scroll.AddChild(grid);

        // Add signatures in rows of 5
        for (var i = 0; i < sigNames.Length; i += 5)
        {
            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 10,
            };
            grid.AddChild(row);

            for (var j = 0; j < 5 && i + j < sigNames.Length; j++)
            {
                var idx = i + j;
                var sigName = sigNames[idx];
                var texture = _spriteSystem.Frame0(
                    new SpriteSpecifier.Rsi(new ResPath("/Textures/_UU/signatures.rsi"), sigName));

                var btn = new Button
                {
                    MinSize = new(100, 64),
                    MaxSize = new(100, 64),
                };

                var textureRect = new TextureRect
                {
                    Texture = texture,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    Stretch = TextureRect.StretchMode.KeepCentered,
                    MinSize = new(64, 32),
                };
                btn.AddChild(textureRect);

                var localIdx = idx;
                var localName = sigName;
                btn.OnPressed += _ =>
                {
                    _selectedSignature = localName;
                    UpdateSelection();
                    OnSignatureSelected?.Invoke(_selectedSignature);
                };

                _signatureButtons[localIdx] = btn;
                row.AddChild(btn);
            }
        }
    }

    public void SetSelected(string? signature)
    {
        _selectedSignature = signature;
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        var sigNames = new[]
        {
            "sig01", "sig02", "sig03", "sig04", "sig05",
            "sig06", "sig07", "sig08", "sig09", "sig10"
        };
        for (var i = 0; i < _signatureButtons.Length; i++)
        {
            _signatureButtons[i].Pressed = sigNames[i] == _selectedSignature;
        }
    }
}
