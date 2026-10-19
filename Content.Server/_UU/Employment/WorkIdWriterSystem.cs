using Content.Shared._UU.Employment;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server._UU.Employment;

/// <summary>
///     Ручной работодатель: пока в руках держат подписанный договор, сканер переносит
///     данные с договора на пластиковое удостоверение. Сама карта не редактируется —
///     источником данных всегда остаётся договор, а подделать можно только договор.
/// </summary>
public sealed class WorkIdWriterSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WorkIdWriterComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<WorkIdWriterComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<WorkIdCardComponent>(target))
            return;

        // Договор ищем в руках: устройство пишет данные только «из договора в руке».
        EmploymentContractComponent? contract = null;
        foreach (var held in _hands.EnumerateHeld(args.User))
        {
            if (!TryComp<EmploymentContractComponent>(held, out var found))
                continue;

            contract = found;
            break;
        }

        if (contract == null)
        {
            _popup.PopupEntity(Loc.GetString("work-id-needs-contract"), args.User, args.User);
            return;
        }

        if (!contract.Complete)
        {
            _popup.PopupEntity(Loc.GetString("work-id-contract-unsigned"), args.User, args.User);
            return;
        }

        if (!TryComp<WorkIdCardComponent>(target, out var card))
            return;

        if (card.Issued)
        {
            _popup.PopupEntity(Loc.GetString("work-id-already-issued"), args.User, args.User);
            return;
        }

        card.Holder = contract.Worker;
        card.Position = contract.Position;
        card.Employer = contract.Employer;
        card.Date = contract.Date;
        card.EmployerSignature = contract.EmployerSignature;
        card.EmployerSignatureLabel = contract.EmployerSignatureLabel;
        card.Issued = true;
        Dirty(target, card);

        _meta.SetEntityName(target, Loc.GetString("work-id-name",
            ("holder", (object)FormattedMessage.EscapeText(contract.Worker ?? "???"))));

        _popup.PopupEntity(Loc.GetString("work-id-issued",
            ("holder", (object)FormattedMessage.EscapeText(contract.Worker ?? "???"))), args.User, args.User);

        args.Handled = true;
    }
}
