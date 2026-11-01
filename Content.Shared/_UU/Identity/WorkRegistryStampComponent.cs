using Robust.Shared.GameStates;

namespace Content.Shared._UU.Identity;

/// <summary>
/// Государственная печать реестра: ставится на подписанное удостоверение,
/// без неё сканер должности карту не читает. Печать многоразовая.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WorkRegistryStampComponent : Component
{
    /// <summary>Государство, за которым закреплена печать (null — не закреплена).</summary>
    [DataField, AutoNetworkedField]
    public int? FactionId;

    /// <summary>Название государства для штампа.</summary>
    [DataField, AutoNetworkedField]
    public string? FactionName;
}
