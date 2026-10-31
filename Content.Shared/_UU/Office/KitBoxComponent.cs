using Robust.Shared.Prototypes;

namespace Content.Shared._UU.Office;

/// <summary>
///     Коробка с деталями набора: при использовании выдаёт содержимое и исчезает.
///     Нужна, чтобы стартовый набор приходил одним предметом, а не пачкой.
/// </summary>
[RegisterComponent]
public sealed partial class KitBoxComponent : Component
{
    /// <summary>
    ///     Что лежит в коробке. Внутри — материалы и готовые инструменты.
    /// </summary>
    [DataField]
    public List<EntProtoId> Contents = new();
}
