using Content.Client.Eui;
using Content.Shared._UU.Cell;
using Content.Shared.Eui;
using Robust.Client.Graphics;

namespace Content.Client._UU.Cell;

public sealed class CellInviteEui : BaseEui
{
    private CellInviteWindow _window = default!;

    // Отказ нужно отправить ровно один раз: иначе кнопка + OnClose шлют два Decline.
    private bool _choiceSent;

    public override void Opened()
    {
        _window = new CellInviteWindow();

        _window.DeclineButton.OnPressed += _ =>
        {
            SendChoice(CellInviteButton.Decline);
            _window.Close();
        };

        _window.AcceptButton.OnPressed += _ =>
        {
            SendChoice(CellInviteButton.Accept);
            _window.Close();
        };

        _window.OnClose += () => SendChoice(CellInviteButton.Decline);

        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    private void SendChoice(CellInviteButton button)
    {
        if (_choiceSent)
            return;

        _choiceSent = true;
        SendMessage(new CellInviteChoiceMessage(button));
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
