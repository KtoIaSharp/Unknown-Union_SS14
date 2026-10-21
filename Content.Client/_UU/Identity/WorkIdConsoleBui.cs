using System;
using Content.Shared._UU.Identity;
using Robust.Client.UserInterface;

namespace Content.Client._UU.Identity;

/// <summary>
///     Клиентская часть консоли работодателя: главное окно и окно настройки доступа.
/// </summary>
public sealed class WorkIdConsoleBui : BoundUserInterface
{
    private WorkIdConsoleWindow? _window;

    public WorkIdConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new WorkIdConsoleWindow();
        _window.OnApply += (holder, position, employer, workplace, tag, applyPhoto) =>
            SendMessage(new WorkIdConsoleApplyMessage(holder, position, employer, workplace, tag, applyPhoto));
        _window.OnAccessSave += (useTags, editTags) =>
            SendMessage(new WorkIdConsoleAccessMessage(useTags, editTags));
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is WorkIdConsoleBuiState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
    }
}
