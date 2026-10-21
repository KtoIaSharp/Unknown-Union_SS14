using Content.Server.EUI;
using Content.Shared._UU.Office;
using Content.Shared.Eui;

namespace Content.Server._UU.Office.Eui;

/// <summary>
///     Окно ввода строки на сервере: заменяет стоковый QuickDialog,
///     чтобы все окна наших систем были в собственном стиле UU.
/// </summary>
public sealed class TextPromptEui : BaseEui
{
    private readonly TextPromptEuiState _state;
    private readonly Action<string> _onSubmit;
    private readonly Action? _onCancel;

    private bool _handled;

    public TextPromptEui(TextPromptEuiState state, Action<string> onSubmit, Action? onCancel = null)
    {
        _state = state;
        _onSubmit = onSubmit;
        _onCancel = onCancel;
    }

    public override EuiStateBase GetNewState()
    {
        return _state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case TextPromptSubmitMessage submit:
                Complete(() => _onSubmit(submit.Text.Trim()));
                return;
            case TextPromptCancelMessage:
                Complete(_onCancel);
                return;
            default:
                Close();
                return;
        }
    }

    private void Complete(Action? action)
    {
        if (_handled)
            return;

        _handled = true;
        action?.Invoke();
        Close();
    }
}
