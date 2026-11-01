using Robust.Shared.GameStates;

namespace Content.Shared._UU.Identity;

/// <summary>
/// Государственная печать реестра: ставится на подписанное удостоверение,
/// без неё сканер должности карту не читает. Сама печать не расходуется.
/// Государство берётся из FactionBoundComponent — привязка общая для консоли, сканера и печати.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WorkRegistryStampComponent : Component
{
}
