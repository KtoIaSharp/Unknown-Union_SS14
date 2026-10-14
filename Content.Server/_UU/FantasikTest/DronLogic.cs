using Content.Shared._UU.Radio;
using Content.Shared.Destructible;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._UU.FantasikTest;

/// <summary>
/// Быстрый FPV-дрон. Сам по себе он ничем не управляет: пока оператор держит
/// пульт в руке, его ввод идёт на дрон, а взгляд — с камеры дрона.
/// </summary>
[RegisterComponent]
public sealed partial class FpvDroneComponent : Component
{
    /// <summary>
    /// Оператор, чей ввод и взгляд сейчас перенаправлены на дрон.
    /// </summary>
    [DataField]
    public EntityUid? Pilot;

    /// <summary>
    /// Пульт, с которого дрон был развёрнут.
    /// </summary>
    [DataField]
    public EntityUid? Controller;

    /// <summary>
    /// Максимальная дистанция между оператором и дроном в тайлах.
    /// Дальше связь рвётся и дрон перестаёт слушаться.
    /// </summary>
    [DataField]
    public float SignalRange = 25f;
}

/// <summary>
/// Пульт FPV-дрона. Z разворачивает дрон, повторное нажатие включает и
/// выключает режим управления.
/// </summary>
[RegisterComponent]
public sealed partial class FpvDroneControllerComponent : Component
{
    /// <summary>
    /// Дрон, закреплённый за этим пультом.
    /// </summary>
    [DataField]
    public EntityUid? Drone;

    /// <summary>
    /// Прототип дрона, который разворачивается при первом использовании.
    /// </summary>
    [DataField]
    public EntProtoId DroneProto = "FpvDrone";

    /// <summary>
    /// Пауза перед развёртыванием нового дрона после потери старого.
    /// </summary>
    [DataField]
    public TimeSpan RedeployCooldown = TimeSpan.FromSeconds(15);

    [ViewVariables]
    public TimeSpan NextDeploy;
}

/// <summary>
/// Маркер оператора, который сейчас управляет дроном.
/// Пока он висит на сущности — её ввод уходит на дрон, а взгляд смотрит с его
/// камеры. Снятие компонента разрывает связь и возвращает управление телом.
/// </summary>
[RegisterComponent]
public sealed partial class FpvDronePilotComponent : Component
{
    [DataField]
    public EntityUid? Drone;
}

public sealed class FpvDroneSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly SoundPathSpecifier LinkSound = new("/Audio/Ambience/Objects/periodic_beep.ogg");

    /// <summary>
    /// Как часто проверяем глушилки и дальность связи.
    /// </summary>
    private const float SignalCheckInterval = 0.5f;

    private float _checkTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FpvDroneControllerComponent, UseInHandEvent>(OnControllerUseInHand);
        SubscribeLocalEvent<FpvDroneComponent, InteractUsingEvent>(OnDroneInteractUsing);

        SubscribeLocalEvent<FpvDronePilotComponent, ComponentShutdown>(OnPilotShutdown);
        SubscribeLocalEvent<FpvDronePilotComponent, MobStateChangedEvent>(OnPilotMobStateChanged);

        SubscribeLocalEvent<FpvDroneComponent, DestructionEventArgs>(OnDroneDestruction);
        SubscribeLocalEvent<FpvDroneComponent, ComponentShutdown>(OnDroneShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _checkTimer += frameTime;
        if (_checkTimer < SignalCheckInterval)
            return;

        _checkTimer = 0f;

        var query = EntityQueryEnumerator<FpvDroneComponent>();
        while (query.MoveNext(out var drone, out var droneComp))
        {
            if (droneComp.Pilot is not { } pilot)
                continue;

            // Связь уже разорвана с другой стороны — просто забываем оператора.
            if (TerminatingOrDeleted(pilot)
                || !TryComp<FpvDronePilotComponent>(pilot, out var pilotComp)
                || pilotComp.Drone != drone)
            {
                droneComp.Pilot = null;
                continue;
            }

            // Пульт выпал, его отобрали или уничтожили — управлять больше нечем.
            if (droneComp.Controller is { } controller
                && (TerminatingOrDeleted(controller) || !_hands.IsHolding(pilot, controller)))
            {
                _popup.PopupEntity(Loc.GetString("fpv-drone-controller-lost"), pilot, pilot, PopupType.MediumCaution);
                RemComp<FpvDronePilotComponent>(pilot);
                continue;
            }

            if (IsJammed(pilot, drone))
            {
                _popup.PopupEntity(Loc.GetString("fpv-drone-signal-jammed"), pilot, pilot, PopupType.MediumCaution);
                RemComp<FpvDronePilotComponent>(pilot);
                continue;
            }

            if (!LinkInRange(pilot, drone, droneComp.SignalRange))
            {
                _popup.PopupEntity(Loc.GetString("fpv-drone-signal-lost"), pilot, pilot, PopupType.MediumCaution);
                RemComp<FpvDronePilotComponent>(pilot);
            }
        }
    }

    // Z по пульту: развернуть дрон либо войти/выйти из режима управления.
    private void OnControllerUseInHand(Entity<FpvDroneControllerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;

        // Уже за штурвалом — выходим.
        if (HasComp<FpvDronePilotComponent>(user))
        {
            RemComp<FpvDronePilotComponent>(user);
            _popup.PopupEntity(Loc.GetString("fpv-drone-link-released"), user, user);
            return;
        }

        // Живой дрон уже привязан — берём управление.
        if (ent.Comp.Drone is { } bound && !TerminatingOrDeleted(bound) && HasComp<FpvDroneComponent>(bound))
        {
            StartPiloting(user, bound, ent);
            return;
        }

        if (_timing.CurTime < ent.Comp.NextDeploy)
        {
            _popup.PopupEntity(Loc.GetString("fpv-drone-deploy-cooldown"), user, user, PopupType.MediumCaution);
            return;
        }

        var drone = SpawnAtPosition(ent.Comp.DroneProto, Transform(user).Coordinates);
        ent.Comp.Drone = drone;

        if (TryComp<FpvDroneComponent>(drone, out var droneComp))
            droneComp.Controller = ent.Owner;

        _popup.PopupEntity(Loc.GetString("fpv-drone-deployed"), user, user);
        StartPiloting(user, drone, ent);
    }

    // Пульт по дрону — привязать дрон к этому пульту.
    private void OnDroneInteractUsing(Entity<FpvDroneComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<FpvDroneControllerComponent>(args.Used, out var controller))
            return;

        args.Handled = true;

        controller.Drone = ent.Owner;
        ent.Comp.Controller = args.Used;

        _popup.PopupEntity(Loc.GetString("fpv-drone-bound"), args.User, args.User);
    }

    private void StartPiloting(EntityUid pilot, EntityUid drone, Entity<FpvDroneControllerComponent> controller)
    {
        if (!TryComp<FpvDroneComponent>(drone, out var droneComp))
            return;

        // Один оператор — один дрон.
        if (HasComp<FpvDronePilotComponent>(pilot))
            return;

        if (droneComp.Pilot is { } otherPilot && otherPilot != pilot)
        {
            _popup.PopupEntity(Loc.GetString("fpv-drone-busy"), pilot, pilot);
            return;
        }

        if (!TryComp<EyeComponent>(pilot, out var eye))
            return;

        var pilotComp = EnsureComp<FpvDronePilotComponent>(pilot);
        pilotComp.Drone = drone;

        droneComp.Pilot = pilot;
        droneComp.Controller = controller.Owner;

        // Ввод тела уходит на дрон, а взгляд — в камеру дрона.
        _mover.SetRelay(pilot, drone);
        _eye.SetDrawFov(pilot, false, eye);
        _eye.SetTarget(pilot, drone, eye);

        _audio.PlayPvs(LinkSound, drone);
        _popup.PopupEntity(Loc.GetString("fpv-drone-link-established"), pilot, pilot);
    }

    // Единственная точка выхода из режима управления.
    private void OnPilotShutdown(Entity<FpvDronePilotComponent> ent, ref ComponentShutdown args)
    {
        var pilot = ent.Owner;

        if (TerminatingOrDeleted(pilot))
            return;

        if (TryComp<EyeComponent>(pilot, out var eye))
        {
            _eye.SetTarget(pilot, null, eye);
            _eye.SetDrawFov(pilot, true, eye);
        }

        RemComp<RelayInputMoverComponent>(pilot);

        if (ent.Comp.Drone is not { } drone || TerminatingOrDeleted(drone))
            return;

        if (TryComp<FpvDroneComponent>(drone, out var droneComp))
            droneComp.Pilot = null;
    }

    // Оператор вырубился — управление теряется.
    private void OnPilotMobStateChanged(Entity<FpvDronePilotComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        RemComp<FpvDronePilotComponent>(ent);
    }

    private void OnDroneDestruction(Entity<FpvDroneComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Pilot is not { } pilot || TerminatingOrDeleted(pilot))
            return;

        _popup.PopupEntity(Loc.GetString("fpv-drone-destroyed"), pilot, pilot, PopupType.LargeCaution);
        RemComp<FpvDronePilotComponent>(pilot);
    }

    private void OnDroneShutdown(Entity<FpvDroneComponent> ent, ref ComponentShutdown args)
    {
        // Отвязываем пульт и запускаем перезарядку на новый дрон.
        if (ent.Comp.Controller is { } controller
            && !TerminatingOrDeleted(controller)
            && TryComp<FpvDroneControllerComponent>(controller, out var controllerComp))
        {
            if (controllerComp.Drone == ent.Owner)
                controllerComp.Drone = null;

            controllerComp.NextDeploy = _timing.CurTime + controllerComp.RedeployCooldown;
        }

        // Страховка, если дрон убрали не через разрушение.
        if (ent.Comp.Pilot is { } pilot
            && !TerminatingOrDeleted(pilot)
            && HasComp<FpvDronePilotComponent>(pilot))
        {
            _popup.PopupEntity(Loc.GetString("fpv-drone-destroyed"), pilot, pilot, PopupType.LargeCaution);
            RemComp<FpvDronePilotComponent>(pilot);
        }
    }

    private bool LinkInRange(EntityUid pilot, EntityUid drone, float range)
    {
        var pilotCoords = _xform.GetMapCoordinates(pilot);
        var droneCoords = _xform.GetMapCoordinates(drone);

        if (pilotCoords.MapId != droneCoords.MapId)
            return false;

        return (pilotCoords.Position - droneCoords.Position).Length() <= range;
    }

    /// <summary>
    /// Общий глушитель рвёт канал управления, если накрывает оператора или дрон.
    /// </summary>
    private bool IsJammed(EntityUid pilot, EntityUid drone)
    {
        var pilotCoords = _xform.GetMapCoordinates(pilot);
        var droneCoords = _xform.GetMapCoordinates(drone);

        var query = EntityQueryEnumerator<JammerComponent, TransformComponent>();
        while (query.MoveNext(out var jammerUid, out var jammer, out var jammerXform))
        {
            if (jammer.JammerType != JammerType.General)
                continue;

            var jammerCoords = _xform.GetMapCoordinates(jammerUid, jammerXform);

            if (jammerCoords.MapId == droneCoords.MapId &&
                (jammerCoords.Position - droneCoords.Position).Length() <= jammer.Radius)
                return true;

            if (jammerCoords.MapId == pilotCoords.MapId &&
                (jammerCoords.Position - pilotCoords.Position).Length() <= jammer.Radius)
                return true;
        }

        return false;
    }
}
