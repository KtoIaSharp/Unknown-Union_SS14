using Content.Server.EUI;
using Content.Shared._UU.Cell;
using Content.Shared.Eui;

namespace Content.Server._UU.CellSystem.Eui;

public sealed class CellInviteEui : BaseEui
{
    private readonly int _cellId;
    private readonly string _cellName;
    private readonly EntityUid _inviter;
    private readonly CellSystem _cellSystem;

    public CellInviteEui(int cellId, string cellName, EntityUid inviter, CellSystem cellSystem)
    {
        _cellId = cellId;
        _cellName = cellName;
        _inviter = inviter;
        _cellSystem = cellSystem;
    }

    public override EuiStateBase GetNewState()
    {
        return new CellInviteEuiState(_cellName, _cellId);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not CellInviteChoiceMessage choice)
        {
            Close();
            return;
        }

        if (choice.Button == CellInviteButton.Decline)
        {
            _cellSystem.DeclineInvite(Player, _inviter);
            Close();
            return;
        }

        _cellSystem.AcceptInvite(Player, _cellName, _cellId, _inviter);
        Close();
    }
}
