using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared._UU.Office;

namespace Content.Server._UU.Office;

/// <summary>
///     Распаковка коробки набора: содержимое падает под ноги, коробка удаляется.
/// </summary>
public sealed class KitBoxSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitBoxComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<KitBoxComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnUseInHand(Entity<KitBoxComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        Unpack(ent, args.User);
        args.Handled = true;
    }

    private void OnActivateInWorld(Entity<KitBoxComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        Unpack(ent, args.User);
        args.Handled = true;
    }

    private void Unpack(Entity<KitBoxComponent> ent, EntityUid user)
    {
        var coords = Transform(ent).Coordinates;

        foreach (var proto in ent.Comp.Contents)
        {
            var spawned = Spawn(proto, coords);

            // Пробуем сразу дать в руки, чтобы мелочь не раскатывалась по полу.
            if (!_hands.TryForcePickupAnyHand(user, spawned))
                Transform(spawned).Coordinates = coords;
        }

        _popup.PopupEntity(Loc.GetString("kit-box-unpacked"), ent, user);
        QueueDel(ent);
    }
}
