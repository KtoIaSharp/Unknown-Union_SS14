using Content.Server._UU.Identity;
using Content.Shared._UU.Employment;
using Content.Shared._UU.Identity;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server._UU.Employment;

/// <summary>
///     Удостоверение о работе: осмотр показывает записанное, взаимодействие открывает
///     окно-карточку с фотографией и подписями.
/// </summary>
public sealed class WorkIdCardSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly WorkRegistrySystem _registry = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WorkIdCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WorkIdCardComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<WorkIdCardComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<WorkIdCardComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnVerb(Entity<WorkIdCardComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("work-id-verb-open"),
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_UU/verb_icons.rsi"), "open_card"),
            Priority = 10,
            Act = () => _ui.OpenUi(ent.Owner, WorkIdCardUiKey.Card, user),
        });
    }

    private void OnUiOpened(Entity<WorkIdCardComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not WorkIdCardUiKey.Card)
            return;

        UpdateBui(ent);
    }

    /// <summary>Использование карты в руке вешает на неё фотографию из второй руки.</summary>
    private void OnUseInHand(Entity<WorkIdCardComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !TryComp<HandsComponent>(args.User, out var hands))
            return;

        foreach (var held in _hands.EnumerateHeld((args.User, hands)))
        {
            if (held == ent.Owner)
                continue;

            if (!TryComp<WorkPhotoComponent>(held, out var photo) || photo.Snapshot == null)
                continue;

            ent.Comp.Photo = photo.Snapshot;
            Dirty(ent.Owner, ent.Comp);
            QueueDel(held);

            _popup.PopupEntity(Loc.GetString("work-card-photo-attached"), args.User, args.User);
            args.Handled = true;
            return;
        }

        _popup.PopupEntity(Loc.GetString("work-card-no-photo-to-attach"), args.User, args.User);
    }

    /// <summary>Перерисовать открытое окно карты (например, после отзыва тега).</summary>
    public void Refresh(EntityUid uid)
    {
        if (TryComp<WorkIdCardComponent>(uid, out var card))
            UpdateBui((uid, card));
    }

    private void UpdateBui(Entity<WorkIdCardComponent> ent)
    {
        var comp = ent.Comp;

        _ui.SetUiState(ent.Owner, WorkIdCardUiKey.Card, new WorkIdCardBuiState(
            comp.Holder,
            comp.Position,
            comp.Employer,
            comp.Date,
            comp.Tag,
            comp.EmployerSignature,
            comp.EmployerSignatureLabel,
            comp.WorkerSignature,
            comp.WorkerSignatureLabel,
            comp.Photo,
            comp.RegistryRevoked || _registry.IsRevoked(comp.Holder, comp.Tag),
            comp.RegistryRevokedBy,
            comp.RegistryRevokedAt,
            comp.RegistryStamped,
            comp.RegistryStampFaction));
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

        if (comp.RegistryStamped)
        {
            args.PushMarkup(Loc.GetString("work-id-examine-stamped",
                ("name", (object)FormattedMessage.EscapeText(
                    comp.RegistryStampFaction ?? Loc.GetString("registry-stamp-no-state")))));
        }
        else
        {
            args.PushMarkup(Loc.GetString("work-id-examine-no-stamp"));
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

        if (_registry.IsRevoked(comp.Holder, comp.Tag))
            args.PushMarkup(Loc.GetString("work-id-examine-revoked"));
    }
}
