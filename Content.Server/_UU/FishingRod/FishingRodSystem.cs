using Content.Shared._UU.FishingRod;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server._UU.FishingRod;

public sealed class FishingRodSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FishingRodComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<FishingRodComponent> rod, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        // Click on entity → pull or release
        if (args.Target is { } target)
        {
            // If already holding something, release it first
            if (rod.Comp.GrabbedEntity != null)
            {
                rod.Comp.GrabbedEntity = null;
                Dirty(rod);
                return;
            }

            // Can't grab yourself
            if (target == args.User)
                return;

            if (!HasComp<PhysicsComponent>(target))
                return;

            var userPos = _xform.GetMapCoordinates(args.User);
            var targetPos = _xform.GetMapCoordinates(target);

            if (userPos.MapId != targetPos.MapId)
                return;

            var delta = userPos.Position - targetPos.Position;
            var distance = delta.Length();

            if (distance > rod.Comp.MaxRange)
                return;

            if (distance < 0.5f)
                return;

            var direction = delta.Normalized();

            // Pull entity toward player
            _throwing.TryThrow(target, direction, rod.Comp.PullSpeed, args.User);

            // Store reference so next click throws it
            rod.Comp.GrabbedEntity = target;
            Dirty(rod);
            args.Handled = true;
            return;
        }

        // Click on empty space → throw grabbed entity there
        if (rod.Comp.GrabbedEntity is { } grabbed && Exists(grabbed))
        {
            var throwPos = _xform.ToMapCoordinates(args.ClickLocation);

            var targetPos = _xform.GetMapCoordinates(grabbed);
            if (throwPos.MapId != targetPos.MapId)
                return;

            var delta = throwPos.Position - targetPos.Position;
            var distance = delta.Length();

            if (distance < 0.5f)
                return;

            var direction = delta.Normalized();
            _throwing.TryThrow(grabbed, direction * distance, rod.Comp.ThrowStrength, args.User);

            rod.Comp.GrabbedEntity = null;
            Dirty(rod);
            args.Handled = true;
        }
    }
}
