using Content.Shared._UU.Employment;
using Content.Shared._UU.Identity;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server._UU.Employment;

/// <summary>
///     Удостоверение о работе: осмотр показывает записанное, взаимодействие открывает
///     окно с полной картой, фотографией и рабочим тегом.
/// </summary>
public sealed class WorkIdCardSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WorkIdCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WorkIdCardComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<WorkIdCardComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    private void OnVerb(Entity<WorkIdCardComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("work-id-verb-open"),
            Priority = 10,
            Act = () => _ui.OpenUi(ent.Owner, WorkIdCardUiKey.Card, user),
        });
    }

    private void OnUiOpened(Entity<WorkIdCardComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not WorkIdCardUiKey.Card)
            return;

        var comp = ent.Comp;
        _ui.SetUiState(ent.Owner, WorkIdCardUiKey.Card, new WorkIdCardBuiState(
            comp.Holder,
            comp.Position,
            comp.Employer,
            comp.Date,
            comp.EmployerSignatureLabel,
            comp.Workplace,
            comp.Tag,
            comp.Photo));
    }

    private void OnExamined(Entity<WorkIdCardComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (!comp.Issued)
        {
            args.PushMarkup(Loc.GetString("work-id-examine-blank"));
            return;
        }

        if (!string.IsNullOrWhiteSpace(comp.Holder))
        {
            args.PushMarkup(Loc.GetString("work-id-examine-holder",
                ("holder", (object)FormattedMessage.EscapeText(comp.Holder!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Position))
        {
            args.PushMarkup(Loc.GetString("work-id-examine-position",
                ("position", (object)FormattedMessage.EscapeText(comp.Position!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Employer))
        {
            args.PushMarkup(Loc.GetString("work-id-examine-employer",
                ("employer", (object)FormattedMessage.EscapeText(comp.Employer!))));
        }

        if (!string.IsNullOrWhiteSpace(comp.Date))
            args.PushMarkup(Loc.GetString("work-id-examine-date", ("date", (object)comp.Date!)));

        if (!string.IsNullOrWhiteSpace(comp.EmployerSignatureLabel))
        {
            var isState = comp.EmployerSignature == EmploymentContractSystem.StateSignature;
            args.PushMarkup(Loc.GetString(isState
                    ? "work-id-examine-signed-state"
                    : "work-id-examine-signed-person",
                ("name", (object)FormattedMessage.EscapeText(comp.EmployerSignatureLabel!))));
        }
    }
}
