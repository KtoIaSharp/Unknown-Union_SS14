using Content.Server.Popups;
using Content.Shared._UU.Employment;
using Content.Shared._UU.Faction;
using Content.Shared._UU.Identity;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._UU.Identity;

/// <summary>
/// Печать реестра: закрепляется за государством и ставится на подписанное удостоверение.
/// Сама печать не расходуется.
/// </summary>
public sealed class WorkRegistryStampSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WorkRegistryStampComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<WorkRegistryStampComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<WorkRegistryStampComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnVerb(Entity<WorkRegistryStampComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.FactionId != null)
            return;

        if (!TryComp<FactionComponent>(args.User, out var faction) || faction.FactionId is not { } factionId)
            return;

        var user = args.User;
        var factionName = faction.Faction;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("work-registry-verb-bind"),
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_UU/verb_icons.rsi"), "bind_faction"),
            Act = () =>
            {
                ent.Comp.FactionId = factionId;
                ent.Comp.FactionName = factionName;
                Dirty(ent, ent.Comp);
                _popup.PopupEntity(
                    Loc.GetString("work-registry-bound", ("name", factionName ?? "???")),
                    ent.Owner,
                    user);
            },
        });
    }

    private void OnAfterInteract(Entity<WorkRegistryStampComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        if (!HasComp<WorkIdCardComponent>(target))
            return;

        if (TryStamp(ent, target, args.User))
            args.Handled = true;
    }

    private void OnUseInHand(Entity<WorkRegistryStampComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // Вторая рука: печать в одной, удостоверение в другой.
        foreach (var held in _hands.EnumerateHeld(args.User))
        {
            if (held == ent.Owner || !HasComp<WorkIdCardComponent>(held))
                continue;

            if (TryStamp(ent, held, args.User))
                args.Handled = true;
            return;
        }
    }

    private bool TryStamp(Entity<WorkRegistryStampComponent> stamp, EntityUid cardUid, EntityUid user)
    {
        if (!TryComp<WorkIdCardComponent>(cardUid, out var card))
            return false;

        if (card.RegistryStamped)
        {
            _popup.PopupEntity(Loc.GetString("registry-stamp-already"), cardUid, user);
            return false;
        }

        if (string.IsNullOrWhiteSpace(card.EmployerSignature) ||
            string.IsNullOrWhiteSpace(card.WorkerSignature))
        {
            _popup.PopupEntity(Loc.GetString("registry-stamp-not-signed"), cardUid, user);
            return false;
        }

        card.RegistryStamped = true;
        card.RegistryStampFaction = stamp.Comp.FactionName;
        card.RegistryStampFactionId = stamp.Comp.FactionId;
        Dirty(cardUid, card);

        _popup.PopupEntity(
            Loc.GetString("registry-stamp-applied",
                ("name", stamp.Comp.FactionName ?? Loc.GetString("registry-stamp-no-state"))),
            cardUid,
            user);
        return true;
    }
}
