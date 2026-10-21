using System;
using Content.Shared._UU.Identity;
using Robust.Client.UserInterface;

namespace Content.Client._UU.Identity;

/// <summary>Окно удостоверения: полная карта, фото и рабочий тег.</summary>
public sealed class WorkIdCardBui : BoundUserInterface
{
    private WorkIdCardWindow? _window;

    public WorkIdCardBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new WorkIdCardWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is WorkIdCardBuiState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
    }
}
