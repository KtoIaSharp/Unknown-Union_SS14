using System.Collections.Generic;
using Content.Shared._UU.Identity;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._UU.Identity;

/// <summary>
///     Фотоаппарат: клик по человеку делает снимок и создаёт предмет-фотографию
///     со «слепком» внешности. Слепок потом переносится на удостоверение консолью.
/// </summary>
public sealed class PhotoCameraSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedVisualBodySystem _body = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextShot = new();

    /// <summary>Слоты одежды, которые попадают на паспортное фото.</summary>
    private static readonly string[] PhotoClothingSlots = { "jumpsuit", "outerClothing" };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PhotoCameraComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PhotoCameraComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PhotoCameraComponent> ent, ref MapInitEvent args)
    {
        _nextShot[ent.Owner] = TimeSpan.Zero;
    }

    private void OnAfterInteract(Entity<PhotoCameraComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<HumanoidProfileComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("photo-camera-no-target"), args.User, args.User);
            return;
        }

        var now = _timing.CurTime;
        if (_nextShot.TryGetValue(ent.Owner, out var readyAt) && now < readyAt)
            return;

        _nextShot[ent.Owner] = now + ent.Comp.Cooldown;

        var snapshot = Capture(target);

        var photo = Spawn("WorkPhoto", Transform(ent).Coordinates);
        if (TryComp<WorkPhotoComponent>(photo, out var photoComp))
        {
            photoComp.Snapshot = snapshot;
            Dirty(photo, photoComp);
        }

        _meta.SetEntityName(photo, Loc.GetString("work-photo-name", ("name", Name(target))));

        if (!_hands.TryForcePickupAnyHand(args.User, photo))
            _popup.PopupEntity(Loc.GetString("photo-camera-taken-floor"), args.User, args.User);
        else
            _popup.PopupEntity(Loc.GetString("photo-camera-taken"), args.User, args.User);

        args.Handled = true;
    }

    private PhotoSnapshot Capture(EntityUid target)
    {
        var snapshot = new PhotoSnapshot
        {
            Name = Name(target),
        };

        if (TryComp<HumanoidProfileComponent>(target, out var profile))
        {
            snapshot.Species = profile.Species;
            snapshot.Sex = profile.Sex;
            snapshot.Gender = profile.Gender;
            snapshot.Age = profile.Age;
        }

        // Причёска и борода — это слои марок; косметику и татуировки не берём.
        var photoLayers = new HashSet<HumanoidVisualLayers>
        {
            HumanoidVisualLayers.Hair,
            HumanoidVisualLayers.FacialHair,
        };

        if (_body.TryGatherMarkingsData(target, photoLayers, out _, out _, out var applied))
        {
            foreach (var organMarkings in applied.Values)
            {
                if (snapshot.HairStyle == null
                    && organMarkings.TryGetValue(HumanoidVisualLayers.Hair, out var hair)
                    && hair.Count > 0)
                {
                    snapshot.HairStyle = hair[0].MarkingId.Id;
                    snapshot.HairColor = hair[0].MarkingColors.Count > 0 ? hair[0].MarkingColors[0] : Color.White;
                }

                if (snapshot.FacialHairStyle == null
                    && organMarkings.TryGetValue(HumanoidVisualLayers.FacialHair, out var facial)
                    && facial.Count > 0)
                {
                    snapshot.FacialHairStyle = facial[0].MarkingId.Id;
                    snapshot.FacialHairColor = facial[0].MarkingColors.Count > 0 ? facial[0].MarkingColors[0] : Color.White;
                }
            }
        }

        foreach (var slot in PhotoClothingSlots)
        {
            if (!_inventory.TryGetSlotEntity(target, slot, out var item))
                continue;

            if (TryComp<MetaDataComponent>(item, out var meta) && meta.EntityPrototype != null)
                snapshot.Clothing.Add(meta.EntityPrototype.ID);
        }

        return snapshot;
    }
}
