using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._UU.Radio;

[RegisterComponent, NetworkedComponent]
public sealed partial class SignalTowerComponent : Component
{
    [DataField]
    public string? Frequency;
}
