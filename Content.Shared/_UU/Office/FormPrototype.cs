using Robust.Shared.Prototypes;

namespace Content.Shared._UU.Office;

/// <summary>
///     Какой документ печатает форма. Значение выбирает обработчик:
///     у государственных бумаг это <c>FactionSystem</c>, у трудового договора — <c>EmploymentContractSystem</c>.
/// </summary>
public enum FormKind : byte
{
    /// <summary>Учредительный документ государства.</summary>
    FactionFounding,

    /// <summary>Документ о приёме в граждане.</summary>
    FactionEntry,

    /// <summary>Правовой документ государства.</summary>
    FactionGovern,

    /// <summary>Документ о выходе из гражданства.</summary>
    FactionLeave,

    /// <summary>Учредительный документ ячейки.</summary>
    FactionCell,

    /// <summary>Договор о трудоустройстве.</summary>
    EmploymentContract,
}

/// <summary>
///     Форма для печатной машинки: лист бумаги остаётся листом, а свойства документа
///     появляются в момент печати. Новая форма — это запись в YAML, а не новый верб.
/// </summary>
[Prototype("form")]
public sealed partial class FormPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Локализованное название формы в списке машинки.</summary>
    [DataField("name", required: true)]
    public string Name = string.Empty;

    /// <summary>
    ///     Локализованное пояснение: что это за документ, кто может его напечатать
    ///     и что он даёт. Показывается под названием формы в списке.
    /// </summary>
    [DataField("description", required: true)]
    public string Description = string.Empty;

    /// <summary>Что именно печатаем.</summary>
    [DataField("kind", required: true)]
    public FormKind Kind;

    /// <summary>Порядок в списке: меньше — выше.</summary>
    [DataField("order")]
    public int Order;
}
