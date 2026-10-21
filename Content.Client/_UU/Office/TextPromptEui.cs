using Content.Client.Eui;
using Content.Shared._UU.Office;
using Content.Shared.Eui;
using Robust.Client.Graphics;

namespace Content.Client._UU.Office;

/// <summary>
///     Клиентская часть окна ввода строки: показывает стилизованное окно вместо QuickDialog.
/// </summary>
public sealed class TextPromptEui : BaseEui
{
    private readonly TextPromptWindow _window = new();

    public TextPromptEui()
    {
        _window.OnSubmitted += text =>
        {
            SendMessage(new TextPromptSubmitMessage(text));
            _window.Close();
        };

        _window.OnCancelled += () =>
        {
            SendMessage(new TextPromptCancelMessage());
            _window.Close();
        };

        _window.OnClose += () =>
        {
            SendMessage(new TextPromptCancelMessage());
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not TextPromptEuiState promptState)
            return;

        _window.ApplyState(
            promptState.Title,
            promptState.Prompt,
            promptState.Placeholder,
            promptState.MaxLength,
            promptState.SubmitLabel,
            promptState.CancelLabel);
    }

    public override void Closed()
    {
        _window.Close();
    }
}
