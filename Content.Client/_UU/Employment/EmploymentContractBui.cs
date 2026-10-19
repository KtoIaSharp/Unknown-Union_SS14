using Content.Shared._UU.Employment;
using Robust.Client.UserInterface;

namespace Content.Client._UU.Employment;

public sealed class EmploymentContractBui : BoundUserInterface
{
    private EmploymentContractWindow? _window;

    public EmploymentContractBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new EmploymentContractWindow();
        _window.OnFieldsSubmitted += (worker, employer, position) =>
        {
            SendMessage(new EmploymentContractFieldsMessage(worker, employer, position));
        };
        _window.OnSignRequested += (asEmployer, asState) =>
        {
            SendMessage(new EmploymentContractSignMessage(asEmployer, asState));
        };
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is EmploymentContractBuiState contractState)
            _window?.UpdateState(contractState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
    }
}
