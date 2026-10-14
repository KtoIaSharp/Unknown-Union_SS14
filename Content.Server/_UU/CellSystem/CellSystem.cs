using Content.Server._UU.CellSystem.Eui;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared._UU.Cell;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._UU.CellSystem;

public sealed class CellSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Кулдаун между приглашениями в ячейку с одного аккаунта — защита от спама окнами EUI.
    /// </summary>
    private static readonly TimeSpan InviteCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Максимальная длина названия ячейки — защита от гигантских строк и markup-инъекций.
    /// </summary>
    private const int MaxCellNameLength = 48;

    /// <summary>
    /// Разумный предел одновременно существующих ячеек, чтобы их нельзя было наплодить бесконечно.
    /// </summary>
    private const int MaxCells = 64;

    private readonly Dictionary<NetUserId, TimeSpan> _inviteCooldowns = new();

    private int _nextCellId = 1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnCellVerb);
        SubscribeLocalEvent<CellComponent, ComponentInit>(OnCellInit);
        SubscribeLocalEvent<CellComponent, ComponentShutdown>(OnCellShutdown);
        SubscribeLocalEvent<CellComponent, LeaveCellActionEvent>(OnLeaveCell);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        // Нумерация и кулдауны — серверные синглтоны, их нельзя тащить в новый раунд.
        _nextCellId = 1;
        _inviteCooldowns.Clear();
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
        _popup.PopupEntity(Loc.GetString("cell-left", ("name", (object)Escape(name))), ent.Owner, ent.Owner);

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
            args.PushMarkup(Loc.GetString("cell-examine-member", ("name", (object)Escape(ent.Comp.CellName))));
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

        // Сессия приглашающего нужна для кулдауна приглашений.
        if (!TryComp<ActorComponent>(inviter, out var inviterActor))
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

                // Не даём спамить окнами приглашений с одного аккаунта.
                var inviterId = inviterActor.PlayerSession.UserId;
                if (_inviteCooldowns.TryGetValue(inviterId, out var lastInvite) &&
                    _timing.CurTime < lastInvite + InviteCooldown)
                {
                    var secondsLeft = (int)Math.Ceiling((lastInvite + InviteCooldown - _timing.CurTime).TotalSeconds);
                    _popup.PopupEntity(Loc.GetString("cell-invite-cooldown", ("seconds", (object)secondsLeft)), inviter, inviter);
                    return;
                }

                var eui = new CellInviteEui(cellId, cellName, inviter, this);
                _euiManager.OpenEui(eui, targetActor.PlayerSession);
                _inviteCooldowns[inviterId] = _timing.CurTime;
            },
        });
    }

    public void AcceptInvite(ICommonSession? player, string cellName, int cellId, EntityUid inviter)
    {
        if (player == null || player.AttachedEntity is not { } uid)
            return;

        // Приглашение могло устареть: приглашающий должен всё ещё существовать
        // и состоять ровно в той же ячейке, иначе получится «сиротское» членство.
        if (!Exists(inviter))
            return;

        if (!TryComp<CellComponent>(inviter, out var inviterCell) || inviterCell.CellId != cellId)
            return;

        if (TryComp<CellComponent>(uid, out var existing) && existing.CellId != null)
            return;

        var cellComp = EnsureComp<CellComponent>(uid);
        cellComp.CellName = cellName;
        cellComp.CellId = cellId;
        Dirty(uid, cellComp);

        _popup.PopupEntity(Loc.GetString("cell-accepted", ("name", (object)Escape(cellName))), uid, uid);
        _popup.PopupEntity(Loc.GetString("cell-accepted-other", ("name", (object)Escape(Name(uid))), ("cell", (object)Escape(cellName))), inviter, inviter);
    }

    public void DeclineInvite(ICommonSession? player, EntityUid inviter)
    {
        if (player == null || player.AttachedEntity is not { } uid)
            return;

        _popup.PopupEntity(Loc.GetString("cell-declined"), uid, uid);
        if (Exists(inviter))
            _popup.PopupEntity(Loc.GetString("cell-declined-other", ("name", (object)Escape(Name(uid)))), inviter, inviter);
    }

    public bool CanCreateCell(EntityUid player)
    {
        return !TryComp<CellComponent>(player, out var existing) || existing.CellId == null;
    }

    public void CreateCell(EntityUid player, string cellName)
    {
        CreateCell(player, cellName, out _);
    }

    /// <summary>
    /// Создаёт ячейку и сообщает наружу, получилось ли это (лимит ячеек может не дать).
    /// </summary>
    public bool CreateCell(EntityUid player, string cellName, out string finalName)
    {
        // Не даём наплодить бесконечно много ячеек.
        if (CountCells() >= MaxCells)
        {
            _popup.PopupEntity(Loc.GetString("cell-limit-reached"), player, player);
            finalName = cellName;
            return false;
        }

        // Название — свободный текст от игрока: режем длину (markup экранируется при выводе).
        if (cellName.Length > MaxCellNameLength)
            cellName = cellName[..MaxCellNameLength];

        finalName = cellName;

        var cellId = _nextCellId++;

        var cellComp = EnsureComp<CellComponent>(player);
        cellComp.CellName = cellName;
        cellComp.CellId = cellId;
        Dirty(player, cellComp);

        _popup.PopupEntity(Loc.GetString("cell-created", ("name", (object)Escape(cellName))), player, player);
        return true;
    }

    private int CountCells()
    {
        var count = 0;
        var query = EntityQueryEnumerator<CellComponent>();
        while (query.MoveNext(out _, out var cell))
        {
            if (cell.CellId != null)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Экранирует пользовательский текст перед вставкой в markup-сообщения (popup/examine).
    /// </summary>
    private static string Escape(string? text)
    {
        return FormattedMessage.EscapeText(text ?? string.Empty);
    }
}
