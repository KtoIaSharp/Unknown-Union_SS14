using Content.Client.Eui;
using Content.Shared._UU.Cell;
using Content.Shared.Eui;
using Robust.Client.Graphics;

namespace Content.Client._UU.Cell;

public sealed class CellInviteEui : BaseEui
{
    private CellInviteWindow _window = default!;

    public override void Opened()
    {
        _window = new CellInviteWindow("...");

        _window.DeclineButton.OnPressed += _ =>
        {
            SendMessage(new CellInviteChoiceMessage(CellInviteButton.Decline));
            _window.Close();
        };

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new CellInviteChoiceMessage(CellInviteButton.Accept));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new CellInviteChoiceMessage(CellInviteButton.Decline));

        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is CellInviteEuiState inviteState)
        {
            _window.Title = Loc.GetString("cell-invite-title");
            _window.Prompt.Text = Loc.GetString("cell-invite-prompt", ("name", (object)inviteState.CellName));
        }
    }

    public override void Closed()
    {
        _window?.Close();
        _window = null!;
    }
}
