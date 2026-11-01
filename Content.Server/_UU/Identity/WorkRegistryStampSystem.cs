using Content.Server._UU.Employment;
using Content.Server._UU.FactionSystem;
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
    [Dependency] private readonly FactionBoundSystem _factionBound = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WorkRegistryStampComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<WorkRegistryStampComponent, UseInHandEvent>(OnUseInHand);
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

        if (!_factionBound.TryGetFaction(stamp.Owner, out var stampFactionId, out var stampFactionName))
        {
            _popup.PopupEntity(Loc.GetString("registry-stamp-no-state"), cardUid, user);
            return false;
        }

        if (card.EmployerSignature == EmploymentContractSystem.StateSignature &&
            stampFactionName is { } stampState &&
            !string.Equals(stampState, card.EmployerSignatureLabel, StringComparison.OrdinalIgnoreCase))
        {
            _popup.PopupEntity(Loc.GetString("registry-stamp-wrong-state"), cardUid, user);
            return false;
        }

        card.RegistryStamped = true;
        card.RegistryStampFaction = stampFactionName;
        card.RegistryStampFactionId = stampFactionId;
        Dirty(cardUid, card);

        _popup.PopupEntity(
            Loc.GetString("registry-stamp-applied",
                ("name", stampFactionName ?? Loc.GetString("registry-stamp-no-state"))),
            cardUid,
            user);
        return true;
    }
}
