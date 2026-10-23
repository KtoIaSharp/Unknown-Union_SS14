using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server._UU.Identity;

/// <summary>
///     Стартовый набор UU: ручка, листы бумаги, отвёртка, фотоаппарат и коробка
///     с печатной машинкой. Набор кладётся в рюкзак, а если рюкзака нет — в руки.
/// </summary>
public sealed class StarterKitSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private static readonly string[] KitItems = { "Pen", "Screwdriver", "PhotoCamera", "TypewriterKit" };
    private const int PaperSheets = 15;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var mob = args.Mob;
        if (!Exists(mob))
            return;

        var coords = Transform(mob).Coordinates;
        var items = new List<EntityUid>(KitItems.Length + PaperSheets);

        foreach (var id in KitItems)
            items.Add(Spawn(id, coords));

        for (var i = 0; i < PaperSheets; i++)
            items.Add(Spawn("Paper", coords));

        // Сначала пробуем рюкзак: он есть у большинства профессий.
        if (_inventory.TryGetSlotEntity(mob, "back", out var back)
            && HasComp<StorageComponent>(back)
            && _container.TryGetContainer(back.Value, StorageComponent.ContainerId, out var storage))
        {
            foreach (var item in items)
            {
                if (!_container.Insert(item, storage))
                    _hands.TryForcePickupAnyHand(mob, item);
            }

            return;
        }

        foreach (var item in items)
            _hands.TryForcePickupAnyHand(mob, item);
    }
}
