using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._UU.Cell;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CellComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? CellName;

    [DataField, AutoNetworkedField]
    public int? CellId;

    [DataField]
    public EntProtoId LeaveCellAction = "ActionLeaveCell";

    [DataField, AutoNetworkedField]
    public EntityUid? LeaveCellActionEntity;
}
