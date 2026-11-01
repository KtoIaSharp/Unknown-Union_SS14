using System;
using Content.Shared._UU.Identity;
using Robust.Client.UserInterface;

namespace Content.Client._UU.Identity;

/// <summary>Окно консоли управления должностями и окно списков доступа.</summary>
public sealed class WorkRegistryBui : BoundUserInterface
{
    private WorkRegistryWindow? _window;
    private WorkRegistryAccessWindow? _access;

    public WorkRegistryBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<WorkRegistryWindow>();
        _window.OnRevokeRequested += entry =>
            SendMessage(new WorkRegistryRevokeMessage(entry.Name, entry.Position, entry.Tag, entry.Revoke));
        _window.OnAccessRequested += OpenAccess;
    }

    private void OpenAccess()
    {
        if (_access != null)
        {
            _access.MoveToFront();
            return;
        }

        _access = new WorkRegistryAccessWindow();
        _access.OnSaved += (use, edit) =>
        {
            SendMessage(new WorkRegistryAccessMessage(use, edit));
            _access?.Close();
        };
        _access.OnClose += () => _access = null;
        _access.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not WorkRegistryBuiState registry)
            return;

        _window?.UpdateState(registry);
        _access?.UpdateState(registry);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _access?.Dispose();
        _access = null;
    }
}
