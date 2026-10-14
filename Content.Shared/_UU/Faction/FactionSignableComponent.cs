using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._UU.Faction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FactionSignableComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<SignatureInfo> Signatures = new();
}
