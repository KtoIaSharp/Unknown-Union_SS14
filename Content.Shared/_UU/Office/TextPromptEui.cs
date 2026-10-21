using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Office;

/// <summary>
///     Состояние универсального окна ввода строки: заголовок, пояснение, подсказка поля.
///     Используется вместо стокового QuickDialog везде, где документы требуют ввода текста.
/// </summary>
[Serializable, NetSerializable]
public sealed class TextPromptEuiState : EuiStateBase
{
    public readonly string Title;
    public readonly string Prompt;
    public readonly string Placeholder;
    public readonly int MaxLength;
    public readonly string SubmitLabel;
    public readonly string CancelLabel;

    public TextPromptEuiState(string title, string prompt, string placeholder, int maxLength,
        string submitLabel, string cancelLabel)
    {
        Title = title;
        Prompt = prompt;
        Placeholder = placeholder;
        MaxLength = maxLength;
        SubmitLabel = submitLabel;
        CancelLabel = cancelLabel;
    }
}

/// <summary>Игрок подтвердил ввод.</summary>
[Serializable, NetSerializable]
public sealed class TextPromptSubmitMessage : EuiMessageBase
{
    public readonly string Text;

    public TextPromptSubmitMessage(string text)
    {
        Text = text;
    }
}

/// <summary>Игрок закрыл окно или нажал «Отмена».</summary>
[Serializable, NetSerializable]
public sealed class TextPromptCancelMessage : EuiMessageBase
{
}
