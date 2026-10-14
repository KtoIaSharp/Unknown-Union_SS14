using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Cell;

[Serializable, NetSerializable]
public sealed class CellInviteEuiState : EuiStateBase
{
    public string CellName;
    public int CellId;

    public CellInviteEuiState(string cellName, int cellId)
    {
        CellName = cellName;
        CellId = cellId;
    }
}

[Serializable, NetSerializable]
public sealed class CellInviteChoiceMessage : EuiMessageBase
{
    public readonly CellInviteButton Button;

    public CellInviteChoiceMessage(CellInviteButton button)
    {
        Button = button;
    }
}

[Serializable, NetSerializable]
public enum CellInviteButton : byte
{
    Accept,
    Decline,
}
