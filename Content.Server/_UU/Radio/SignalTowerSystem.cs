using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._UU.Radio;
using Content.Server.Power.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._UU.Radio;

/// <summary>
/// Сигнальная вышка: доступ по коду и видимое состояние (стоит / ретранслирует / заглушена / обесточена).
///
/// Код — не «владелец», а пропуск в узкий круг доверенных лиц: кто первым откроет вышку,
/// тот и задаёт код, а дальше вышка пускает только тех, кто этот код знает.
/// Код хранится исключительно на сервере, клиент видит лишь факт «заперто».
/// </summary>
public sealed class SignalTowerSystem : EntitySystem
{
    [Dependency] private readonly RadioChannelSystem _radio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private const int MinCodeLength = 3;
    private const int MaxCodeLength = 12;
    private const int MaxFrequencyLength = 3;

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SignalTowerComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<SignalTowerComponent, TowerConfigureMessage>(OnConfigure);
        SubscribeLocalEvent<SignalTowerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SignalTowerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<SignalTowerComponent> ent, ref ComponentInit args)
    {
        SetState(ent.Owner, SignalTowerState.Idle);
    }

    private void OnPowerChanged(Entity<SignalTowerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        // Цепочка ретрансляции разрешается один раз на пару «частота + карта»,
        // а не на каждую вышку: иначе цена растёт квадратично.
        var activeByFrequency = new Dictionary<(string Freq, MapId Map), HashSet<EntityUid>>();

        var query = EntityQueryEnumerator<SignalTowerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tower, out _))
        {
            if (tower.Frequency == null)
            {
                SetState(uid, SignalTowerState.Idle);
                continue;
            }

            if (!_power.IsPowered(uid))
            {
                SetState(uid, SignalTowerState.Off);
                continue;
            }

            var coords = _radio.GetMapCoordinates(uid);
            var key = (tower.Frequency, coords.MapId);

            if (!activeByFrequency.TryGetValue(key, out var active))
            {
                active = _radio.GetActiveTowers(tower.Frequency, coords.MapId);
                activeByFrequency[key] = active;
            }

            var state = active.Contains(uid) ? SignalTowerState.Active : SignalTowerState.Idle;

            if (_radio.IsJammedAt(tower.Frequency, coords))
                state = SignalTowerState.Jammed;

            SetState(uid, state);
        }
    }

    private void OnBuiOpened(Entity<SignalTowerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not FrequencyUiKey.Tower)
            return;

        UpdateBui(ent);
    }

    private void OnConfigure(Entity<SignalTowerComponent> ent, ref TowerConfigureMessage args)
    {
        var code = args.Code.Trim();
        var freq = args.Frequency.Trim();

        // Частота попадает в чат и интерфейс: только буквы и цифры.
        if (string.IsNullOrWhiteSpace(freq)
            || freq.Length > MaxFrequencyLength
            || !freq.All(char.IsLetterOrDigit))
        {
            _popup.PopupEntity(Loc.GetString("radio-invalid-frequency"), args.Actor, args.Actor);
            return;
        }

        var access = EnsureComp<SignalTowerAccessComponent>(ent);

        if (string.IsNullOrEmpty(access.Code))
        {
            // Первый, кто открыл вышку, задаёт код — дальше она пускает только «своих».
            if (code.Length < MinCodeLength
                || code.Length > MaxCodeLength
                || !code.All(char.IsLetterOrDigit))
            {
                _popup.PopupEntity(Loc.GetString("tower-invalid-code"), args.Actor, args.Actor);
                return;
            }

            access.Code = code;
            _popup.PopupEntity(Loc.GetString("tower-code-set"), args.Actor, args.Actor);
        }
        else if (!string.Equals(access.Code, code, StringComparison.OrdinalIgnoreCase))
        {
            _popup.PopupEntity(Loc.GetString("tower-wrong-code"), args.Actor, args.Actor);
            return;
        }

        ent.Comp.Frequency = freq;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("tower-frequency-set", ("freq", freq)), args.Actor, args.Actor);

        UpdateBui(ent);
        UpdateAppearance(ent);
    }

    /// <summary>
    /// Заперта ли вышка кодом (для конфигуратора частот: он код обойти не должен).
    /// </summary>
    public bool IsLocked(EntityUid uid)
    {
        return TryComp<SignalTowerAccessComponent>(uid, out var access)
               && !string.IsNullOrEmpty(access.Code);
    }

    private void UpdateAppearance(Entity<SignalTowerComponent> ent)
    {
        if (ent.Comp.Frequency == null)
        {
            SetState(ent.Owner, SignalTowerState.Idle);
            return;
        }

        if (!_power.IsPowered(ent.Owner))
        {
            SetState(ent.Owner, SignalTowerState.Off);
            return;
        }

        var coords = _radio.GetMapCoordinates(ent.Owner);
        var state = SignalTowerState.Idle;

        if (_radio.GetActiveTowers(ent.Comp.Frequency, coords.MapId).Contains(ent.Owner))
            state = SignalTowerState.Active;

        if (_radio.IsJammedAt(ent.Comp.Frequency, coords))
            state = SignalTowerState.Jammed;

        SetState(ent.Owner, state);
    }

    private void UpdateBui(Entity<SignalTowerComponent> ent)
    {
        var state = new TowerBuiState(ent.Comp.Frequency, IsLocked(ent.Owner));
        _ui.SetUiState(ent.Owner, FrequencyUiKey.Tower, state);
    }

    private void SetState(EntityUid uid, SignalTowerState state)
    {
        _appearance.SetData(uid, SignalTowerVisuals.State, state);
    }
}
