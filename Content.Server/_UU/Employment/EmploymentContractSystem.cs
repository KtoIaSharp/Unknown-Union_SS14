using Content.Server._UU.FactionSystem;
using Content.Shared._UU.Employment;
using Content.Shared._UU.Faction;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._UU.Employment;

/// <summary>
///     Договор о трудоустройстве: окно с полями (кого назначают, кто назначает, должность)
///     и двумя отдельными подписями — от работодателя и от работника.
///     Подпись от имени государства доступна только главе государства.
///     Никакой криптопроверки в коде нет: договор можно подделать — это часть RP.
/// </summary>
public sealed class EmploymentContractSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private const string PenTag = "Pen";

    /// <summary>Метка, которой помечается подпись, поставленная от имени государства.</summary>
    public const string StateSignature = "state";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmploymentContractComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<EmploymentContractComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<EmploymentContractComponent, EmploymentContractFieldsMessage>(OnFields);
        SubscribeLocalEvent<EmploymentContractComponent, EmploymentContractSignMessage>(OnSign);
        SubscribeLocalEvent<EmploymentContractComponent, ExaminedEvent>(OnExamine);
    }

    private void OnVerb(Entity<EmploymentContractComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("employment-contract-verb-open"),
            Priority = 10,
            Act = () => _ui.OpenUi(ent.Owner, EmploymentContractUiKey.Contract, user),
        });
    }

    private void OnBuiOpened(Entity<EmploymentContractComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not EmploymentContractUiKey.Contract)
            return;

        UpdateBui(ent, args.Actor);
    }

    private void OnFields(Entity<EmploymentContractComponent> ent, ref EmploymentContractFieldsMessage args)
    {
        var worker = Clamp(args.Worker);
        var employer = Clamp(args.Employer);
        var position = Clamp(args.Position);

        // Правки в полях обнуляют подписи: подписывают то, что написано.
        var changed = worker != ent.Comp.Worker
                      || employer != ent.Comp.Employer
                      || position != ent.Comp.Position;

        ent.Comp.Worker = worker;
        ent.Comp.Employer = employer;
        ent.Comp.Position = position;

        if (changed)
        {
            ent.Comp.EmployerSignature = null;
            ent.Comp.EmployerSignatureLabel = null;
            ent.Comp.WorkerSignature = null;
            ent.Comp.WorkerSignatureLabel = null;
        }

        if (ent.Comp.Date == null && !string.IsNullOrWhiteSpace(worker))
            ent.Comp.Date = GameDate();

        Dirty(ent);
        RefreshMeta(ent);
        UpdateBui(ent, args.Actor);
    }

    private void OnSign(Entity<EmploymentContractComponent> ent, ref EmploymentContractSignMessage args)
    {
        var user = args.Actor;

        // Подпись ставится только ручкой: без неё договор не подписать.
        if (!HasPen(user))
        {
            _popup.PopupEntity(Loc.GetString("employment-contract-needs-pen"), user, user);
            return;
        }

        if (string.IsNullOrWhiteSpace(ent.Comp.Worker) || string.IsNullOrWhiteSpace(ent.Comp.Position))
        {
            _popup.PopupEntity(Loc.GetString("employment-contract-needs-fields"), user, user);
            return;
        }

        string label;
        string sprite;

        if (args.AsState)
        {
            if (!TryComp<FactionComponent>(user, out var faction)
                || !faction.IsLeader
                || string.IsNullOrWhiteSpace(faction.Faction))
            {
                _popup.PopupEntity(Loc.GetString("employment-contract-not-a-leader"), user, user);
                return;
            }

            // Подпись государства — не росчерк человека, а метка государства на карте.
            label = faction.Faction;
            sprite = StateSignature;
        }
        else
        {
            label = Name(user);
            sprite = SignatureHelper.GetOrAssignSignatureSprite(user, _random, EntityManager);
        }

        if (args.AsEmployer)
        {
            ent.Comp.EmployerSignature = sprite;
            ent.Comp.EmployerSignatureLabel = label;
            ent.Comp.Employer = string.IsNullOrWhiteSpace(ent.Comp.Employer) ? label : ent.Comp.Employer;
        }
        else
        {
            ent.Comp.WorkerSignature = sprite;
            ent.Comp.WorkerSignatureLabel = label;
            ent.Comp.Worker = string.IsNullOrWhiteSpace(ent.Comp.Worker) ? label : ent.Comp.Worker;
        }

        ent.Comp.Date ??= GameDate();
        Dirty(ent);
        RefreshMeta(ent);
        UpdateBui(ent, user);

        _popup.PopupEntity(Loc.GetString("employment-contract-signed", ("name", (object)FormattedMessage.EscapeText(label))), user, user);

        if (ent.Comp.Complete)
            _popup.PopupEntity(Loc.GetString("employment-contract-complete"), user, user);
    }

    private void OnExamine(Entity<EmploymentContractComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (!string.IsNullOrWhiteSpace(comp.Worker))
        {
            args.PushMarkup(Loc.GetString("employment-contract-examine-worker",
                ("worker", (object)FormattedMessage.EscapeText(comp.Worker!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Position))
        {
            args.PushMarkup(Loc.GetString("employment-contract-examine-position",
                ("position", (object)FormattedMessage.EscapeText(comp.Position!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Employer))
        {
            args.PushMarkup(Loc.GetString("employment-contract-examine-employer",
                ("employer", (object)FormattedMessage.EscapeText(comp.Employer!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Date))
            args.PushMarkup(Loc.GetString("employment-contract-examine-date", ("date", (object)comp.Date!)));

        args.PushMarkup(Loc.GetString(comp.Complete
            ? "employment-contract-examine-signed"
            : "employment-contract-examine-unsigned"));
    }

    /// <summary>
    ///     Печать бланка договора на листе из печатной машинки. Лист остаётся бумагой —
    ///     свойства договора появляются здесь, а не в отдельном предмете.
    /// </summary>
    public bool TryPrintContract(Entity<PaperComponent> paper, EntityUid user, out string? error)
    {
        error = null;

        if (HasComp<EmploymentContractComponent>(paper))
        {
            error = "typewriter-form-already";
            return false;
        }

        var contract = AddComp<EmploymentContractComponent>(paper);
        contract.Date = null;

        // У напечатанного договора интерфейса из прототипа нет: бумага — обычный Paper,
        // у которого зарегистрирован только PaperUiKey. Поэтому окно договора подключаем вручную,
        // иначе верб «Открыть договор» молча ничего не открывает.
        _ui.SetUi((paper.Owner, (UserInterfaceComponent?)null), EmploymentContractUiKey.Contract,
            new InterfaceData("EmploymentContractBui"));

        _paper.SetContent(paper, Loc.GetString("employment-contract-body"));
        Dirty(paper, contract);

        _meta.SetEntityName(paper, Loc.GetString("employment-contract-printed-name"));
        _meta.SetEntityDescription(paper, Loc.GetString("employment-contract-printed-description"));

        _popup.PopupEntity(Loc.GetString("employment-contract-printed"), user, user);
        return true;
    }

    /// <summary>Подпись ставится только ручкой — предметом с тегом Pen в руках.</summary>
    private bool HasPen(EntityUid user)
    {
        if (!TryComp<HandsComponent>(user, out var hands))
            return false;

        foreach (var held in _hands.EnumerateHeld((user, hands)))
        {
            if (_tag.HasTag(held, PenTag))
                return true;
        }

        return false;
    }

    private void UpdateBui(Entity<EmploymentContractComponent> ent, EntityUid actor)
    {
        var comp = ent.Comp;
        var canSignAsState = false;
        string? stateName = null;

        if (TryComp<FactionComponent>(actor, out var faction) && !string.IsNullOrWhiteSpace(faction.Faction))
        {
            stateName = faction.Faction;
            canSignAsState = faction.IsLeader;
        }

        var state = new EmploymentContractBuiState(
            comp.Worker,
            comp.Position,
            comp.Employer,
            comp.Date,
            comp.EmployerSignature,
            comp.EmployerSignatureLabel,
            comp.WorkerSignature,
            comp.WorkerSignatureLabel,
            canSignAsState,
            stateName);

        _ui.SetUiState(ent.Owner, EmploymentContractUiKey.Contract, state);
    }

    private void RefreshMeta(Entity<EmploymentContractComponent> ent)
    {
        var comp = ent.Comp;

        _meta.SetEntityDescription(ent.Owner, comp.Complete
            ? Loc.GetString("employment-contract-desc-signed",
                ("worker", (object)(comp.Worker ?? "???")),
                ("position", (object)(comp.Position ?? "???")))
            : Loc.GetString("employment-contract-desc"));
    }

    private string GameDate()
    {
        return new DateTime(2700, 1, 1)
            .AddDays((int)(_timing.CurTime.TotalMinutes / 20))
            .ToString("dd.MM.yyyy");
    }

    private static string? Clamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > EmploymentContractComponent.MaxFieldLength
            ? trimmed[..EmploymentContractComponent.MaxFieldLength]
            : trimmed;
    }
}
