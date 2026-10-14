using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Radio;

[RegisterComponent, NetworkedComponent]
public sealed partial class JammerComponent : Component
{
    [DataField]
    public JammerType JammerType = JammerType.General;

    [DataField]
    public string? TargetFrequency;

    [DataField]
    public float Radius = 20f;
}

[Serializable, NetSerializable]
public enum JammerType
{
    General,
    Specific,
}
