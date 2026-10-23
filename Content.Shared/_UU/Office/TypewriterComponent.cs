using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Office;

/// <summary>
///     Печатная машинка: в валик вставляется обычный или государственный лист бумаги,
///     после чего на нём печатается выбранная форма.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TypewriterComponent : Component
{
    public const string PaperSlotId = "paper";

    /// <summary>Слот под лист бумаги в валике.</summary>
    [DataField]
    public ItemSlot PaperSlot = new();
}

/// <summary>Ключи визуального состояния машинки.</summary>
[Serializable, NetSerializable]
public enum TypewriterVisuals : byte
{
    /// <summary>Три состояния спрайта: пусто, пустой лист, напечатанный лист.</summary>
    State,
}

/// <summary>Что показывает спрайт машинки.</summary>
[Serializable, NetSerializable]
public enum TypewriterState : byte
{
    /// <summary>Валик пуст — листов нет.</summary>
    Idle,

    /// <summary>В валике чистый лист.</summary>
    Loaded,

    /// <summary>В валике лист с напечатанным текстом.</summary>
    Printed,
}

/// <summary>Ключ интерфейса печатной машинки.</summary>
[Serializable, NetSerializable]
public enum TypewriterUiKey : byte
{
    Key,
}

/// <summary>
///     Что клиент знает о машинке: список форм и наличие бумаги.
///     Названия форм клиент берёт прямо из прототипов, поэтому в стейте только идентификаторы.
/// </summary>
[Serializable, NetSerializable]
public sealed class TypewriterBuiState : BoundUserInterfaceState
{
    public readonly List<string> Forms;
    public readonly bool HasPaper;

    public TypewriterBuiState(List<string> forms, bool hasPaper)
    {
        Forms = forms;
        HasPaper = hasPaper;
    }
}

/// <summary>Печать выбранной формы на листе, который лежит в валике.</summary>
[Serializable, NetSerializable]
public sealed class TypewriterPrintMessage : BoundUserInterfaceMessage
{
    public readonly string Form;

    public TypewriterPrintMessage(string form)
    {
        Form = form;
    }
}
