using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._UU.FishingRod;

[RegisterComponent, NetworkedComponent]
public sealed partial class FishingRodComponent : Component
{
    [DataField]
    public float PullSpeed = 25f;

    [DataField]
    public float MaxRange = 10f;

    [DataField]
    public float ThrowStrength = 15f;

    [DataField]
    public EntityUid? GrabbedEntity;
}
