using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._UU.Faction;

/// <summary>
/// Предмет или устройство, закреплённое за государством: печать, сканер, консоль и всё,
/// что понадобится дальше. Привязка ставится один раз и дальше только читается.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FactionBoundComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? FactionId;

    [DataField, AutoNetworkedField]
    public string? FactionName;

    /// <summary>Закреплять может только глава государства, а не любой гражданин.</summary>
    [DataField]
    public bool LeaderOnly;
}
