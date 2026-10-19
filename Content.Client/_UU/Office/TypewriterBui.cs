using Content.Shared._UU.Office;
using Robust.Client.UserInterface;

namespace Content.Client._UU.Office;

/// <summary>
///     Интерфейс печатной машинки: список форм и кнопка печати на вставленном листе.
/// </summary>
public sealed class TypewriterBui : BoundUserInterface
{
    private TypewriterWindow? _window;

    public TypewriterBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new TypewriterWindow();
        _window.OnPrintRequested += form => SendMessage(new TypewriterPrintMessage(form));
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is TypewriterBuiState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
    }
}
