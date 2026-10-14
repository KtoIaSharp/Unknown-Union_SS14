using Content.Server._UU.CellSystem.Eui;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared._UU.Cell;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._UU.CellSystem;

public sealed class CellSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private int _nextCellId = 1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnCellVerb);
        SubscribeLocalEvent<CellComponent, ComponentInit>(OnCellInit);
        SubscribeLocalEvent<CellComponent, ComponentShutdown>(OnCellShutdown);
        SubscribeLocalEvent<CellComponent, LeaveCellActionEvent>(OnLeaveCell);
    }

    private void OnCellInit(Entity<CellComponent> ent, ref ComponentInit args)
    {
        _actions.AddAction(ent, ref ent.Comp.LeaveCellActionEntity, ent.Comp.LeaveCellAction);
    }

    private void OnCellShutdown(Entity<CellComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.LeaveCellActionEntity);
    }

    private void OnLeaveCell(Entity<CellComponent> ent, ref LeaveCellActionEvent args)
    {
        if (ent.Comp.CellId == null)
            return;

        var name = ent.Comp.CellName ?? "???";
        _popup.PopupEntity(Loc.GetString("cell-left", ("name", (object)name)), ent.Owner, ent.Owner);

        RemComp<CellComponent>(ent.Owner);
        args.Handled = true;
    }

    private void OnExamine(Entity<CellComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (string.IsNullOrEmpty(ent.Comp.CellName))
            return;

        if (TryComp<CellComponent>(args.Examiner, out var examinerCell) &&
            examinerCell.CellId == ent.Comp.CellId &&
            examinerCell.CellId != null)
        {
            args.PushMarkup(Loc.GetString("cell-examine-member", ("name", (object)ent.Comp.CellName)));
        }
    }

    private void OnCellVerb(GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var inviter = args.User;
        var target = args.Target;

        if (!TryComp<CellComponent>(inviter, out var inviterCell) || inviterCell.CellId == null)
            return;

        if (!HasComp<ActorComponent>(target))
            return;

        if (TryComp<CellComponent>(target, out var targetCell) && targetCell.CellId != null)
            return;

        if (inviter == target)
            return;

        var cellId = inviterCell.CellId.Value;
        var cellName = inviterCell.CellName ?? "???";

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("verb-cell-invite", ("name", (object)cellName)),
            Priority = 10,
            Act = () =>
            {
                if (!TryComp<ActorComponent>(target, out var targetActor))
                    return;

                var eui = new CellInviteEui(cellId, cellName, inviter, this);
                _euiManager.OpenEui(eui, targetActor.PlayerSession);
            },
        });
    }

    public void AcceptInvite(ICommonSession? player, string cellName, int cellId, EntityUid inviter)
    {
        if (player == null || player.AttachedEntity is not { } uid)
            return;

        if (TryComp<CellComponent>(uid, out var existing) && existing.CellId != null)
            return;

        var cellComp = EnsureComp<CellComponent>(uid);
        cellComp.CellName = cellName;
        cellComp.CellId = cellId;
        Dirty(uid, cellComp);

        _popup.PopupEntity(Loc.GetString("cell-accepted", ("name", (object)cellName)), uid, uid);
        if (Exists(inviter))
            _popup.PopupEntity(Loc.GetString("cell-accepted-other", ("name", (object)Name(uid)), ("cell", (object)cellName)), inviter, inviter);
    }

    public void DeclineInvite(ICommonSession? player, EntityUid inviter)
    {
        if (player == null || player.AttachedEntity is not { } uid)
            return;

        _popup.PopupEntity(Loc.GetString("cell-declined"), uid, uid);
        if (Exists(inviter))
            _popup.PopupEntity(Loc.GetString("cell-declined-other", ("name", (object)Name(uid))), inviter, inviter);
    }

    public bool CanCreateCell(EntityUid player)
    {
        return !TryComp<CellComponent>(player, out var existing) || existing.CellId == null;
    }

    public void CreateCell(EntityUid player, string cellName)
    {
        var cellId = _nextCellId++;

        var cellComp = EnsureComp<CellComponent>(player);
        cellComp.CellName = cellName;
        cellComp.CellId = cellId;

        _popup.PopupEntity(Loc.GetString("cell-created", ("name", (object)cellName)), player, player);
    }
}
