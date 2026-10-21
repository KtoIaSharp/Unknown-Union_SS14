using Content.Server._UU.Office.Eui;
using Content.Server.EUI;
using Content.Shared._UU.Office;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._UU.Office;

/// <summary>
///     Открывает окна ввода строки в стиле UU вместо стокового QuickDialog.
///     Так все окна наших систем выглядят одинаково.
/// </summary>
public sealed class TextPromptSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _eui = default!;

    public void Open(ICommonSession session, TextPromptEuiState state,
        Action<string> onSubmit, Action? onCancel = null)
    {
        _eui.OpenEui(new TextPromptEui(state, onSubmit, onCancel), session);
    }
}
