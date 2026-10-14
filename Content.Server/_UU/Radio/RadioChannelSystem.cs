using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._UU.Radio;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._UU.Radio;

public sealed class RadioChannelSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<string> _frequencies = new();
    private readonly Dictionary<string, string?> _frequencyNames = new();

    // Sources that just broadcast on a frequency — used to recognize the auto-whisper
    // that follows a :FREQ message (its TTS must reach only listeners without the frequency).
    private readonly Dictionary<EntityUid, (TimeSpan Time, List<string> Freqs)> _recentlyBroadcast = new();
    private static readonly TimeSpan BroadcastWindow = TimeSpan.FromSeconds(1);

    public const float BaseRange = 30f;
    public const float TowerRadius = 20f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransmitterComponent, MapInitEvent>(OnTransmitterInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        // Реестр частот и отметки о недавних передачах — серверные синглтоны, их нельзя тащить в новый раунд.
        _frequencies.Clear();
        _frequencyNames.Clear();
        _recentlyBroadcast.Clear();
    }

    private void OnTransmitterInit(Entity<TransmitterComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Frequency != null)
            _frequencies.Add(ent.Comp.Frequency);
    }

    public bool TryRegisterFrequency(string freq, string? name = null)
    {
        if (_frequencies.Add(freq))
        {
            if (name != null)
                _frequencyNames[freq] = name;
            return true;
        }
        return false;
    }

    public bool TryUnregisterFrequency(string freq)
    {
        _frequencyNames.Remove(freq);
        return _frequencies.Remove(freq);
    }

    public string? GetFrequencyName(string freq)
    {
        return _frequencyNames.TryGetValue(freq, out var name) ? name : null;
    }

    /// <summary>
    /// Marks that the source just broadcast on this frequency, so the auto-whisper
    /// that follows a :FREQ message can be recognized.
    /// </summary>
    public void MarkBroadcast(EntityUid sender, string freq)
    {
        var now = _timing.CurTime;
        if (!_recentlyBroadcast.TryGetValue(sender, out var entry) || now - entry.Time > BroadcastWindow)
        {
            _recentlyBroadcast[sender] = (now, new List<string> { freq });
            return;
        }

        if (!entry.Freqs.Contains(freq))
            entry.Freqs.Add(freq);
    }

    /// <summary>
    /// Returns true if the source broadcast on a frequency within the last second.
    /// </summary>
    public bool WasRecentlyBroadcast(EntityUid source, out List<string> freqs)
    {
        freqs = new List<string>();
        if (!_recentlyBroadcast.TryGetValue(source, out var entry))
            return false;

        if (_timing.CurTime - entry.Time > BroadcastWindow)
        {
            _recentlyBroadcast.Remove(source);
            return false;
        }

        freqs = entry.Freqs;
        return true;
    }

    /// <summary>
    /// Returns true if the owner (or anything they carry) has a working device bound to any of the frequencies.
    /// </summary>
    public bool HasFrequency(EntityUid owner, List<string> freqs)
    {
        if (freqs.Count == 0)
            return false;

        var radioQuery = EntityQueryEnumerator<RadioComponent, TransformComponent>();
        while (radioQuery.MoveNext(out var uid, out var comp, out _))
        {
            if (!IsRadioActive(uid) || !BelongsTo(uid, owner))
                continue;
            if (comp.Frequencies.Any(freqs.Contains))
                return true;
        }

        var earpieceQuery = EntityQueryEnumerator<EarpieceComponent, TransformComponent>();
        while (earpieceQuery.MoveNext(out var uid, out var comp, out _))
        {
            if (!IsEarpieceActive(uid) || !BelongsTo(uid, owner))
                continue;
            if (comp.Frequencies.Any(freqs.Contains))
                return true;
        }

        return false;
    }

    private bool BelongsTo(EntityUid uid, EntityUid owner)
    {
        var parent = Transform(uid).ParentUid;
        while (parent.IsValid() && parent != owner)
        {
            if (!TryComp<TransformComponent>(parent, out var parentXform))
                break;
            parent = parentXform.ParentUid;
        }
        return parent == owner;
    }

    /// <summary>
    /// Returns true if the point is inside the transmitter's coverage zone
    /// or inside the zone of any active (chained) tower on this frequency.
    /// Towers must stand strictly inside an existing zone to relay coverage.
    /// </summary>
    public bool IsPointCovered(string freq, MapCoordinates point)
    {
        // Direct transmitter coverage
        if (IsInsideTransmitterZone(freq, point))
            return true;

        // Collect all towers on this frequency and map
        var towerQueue = new List<MapCoordinates>();
        var towerQuery = EntityQueryEnumerator<SignalTowerComponent, TransformComponent>();
        while (towerQuery.MoveNext(out var uid, out var tower, out var xform))
        {
            if (tower.Frequency != freq)
                continue;

            var towerMap = GetMapCoordinates(uid, xform);
            if (towerMap.MapId != point.MapId)
                continue;

            towerQueue.Add(towerMap);
        }

        // Resolve chains: a tower is active if it stands inside the transmitter zone
        // or inside the zone of an already active tower
        var activeTowers = new List<MapCoordinates>();
        var changed = true;
        while (changed)
        {
            changed = false;
            var remaining = new List<MapCoordinates>();
            foreach (var t in towerQueue)
            {
                if (IsInsideTransmitterZone(freq, t) || IsInsideTowerZone(activeTowers, t))
                {
                    activeTowers.Add(t);
                    changed = true;
                }
                else
                {
                    remaining.Add(t);
                }
            }
            towerQueue = remaining;
        }

        // The point is covered if it stands inside any active tower's zone
        return IsInsideTowerZone(activeTowers, point);
    }

    private bool IsInsideTransmitterZone(string freq, MapCoordinates point)
    {
        var query = EntityQueryEnumerator<TransmitterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Frequency != freq || !comp.Configured)
                continue;

            // Unpowered transmitter can't broadcast
            if (!_power.IsPowered(uid))
                continue;

            var mapCoords = GetMapCoordinates(uid, xform);
            if (mapCoords.MapId != point.MapId)
                continue;

            if ((mapCoords.Position - point.Position).Length() < BaseRange)
                return true;
        }
        return false;
    }

    private bool IsInsideTowerZone(List<MapCoordinates> activeTowers, MapCoordinates point)
    {
        foreach (var tower in activeTowers)
        {
            if (tower.MapId != point.MapId)
                continue;

            if ((tower.Position - point.Position).Length() < TowerRadius)
                return true;
        }
        return false;
    }

    public List<(EntityUid Entity, MapCoordinates Coords)> GetReceiversOnFrequency(
        string freq, EntityCoordinates sourceCoords)
    {
        var result = new List<(EntityUid, MapCoordinates)>();

        var radioQuery = EntityQueryEnumerator<RadioComponent, TransformComponent>();
        while (radioQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Frequencies.Contains(freq) && IsRadioActive(uid))
                result.Add((uid, GetMapCoordinates(uid, xform)));
        }

        var earpieceQuery = EntityQueryEnumerator<EarpieceComponent, TransformComponent>();
        while (earpieceQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Frequencies.Contains(freq))
                result.Add((uid, GetMapCoordinates(uid, xform)));
        }

        return result;
    }

    public MapCoordinates GetMapCoordinates(EntityUid uid)
    {
        var xform = Transform(uid);
        return GetMapCoordinates(uid, xform);
    }

    /// <summary>
    /// A radio only works if it's held in a hand or worn on the belt.
    /// </summary>
    public bool IsRadioActive(EntityUid radioUid)
    {
        var parent = Transform(radioUid).ParentUid;
        if (!parent.IsValid())
            return false;

        // Held in a hand
        if (_hands.IsHolding(parent, radioUid))
            return true;

        // Worn on the belt
        if (_inventory.TryGetSlotContainer(parent, "belt", out var slot, out _)
            && slot.ContainedEntity == radioUid)
            return true;

        return false;
    }

    /// <summary>
    /// Гарнитура работает только когда надета в слот ушей.
    /// Иначе её можно было бы использовать для передачи, просто держа в рюкзаке.
    /// </summary>
    public bool IsEarpieceActive(EntityUid earpieceUid)
    {
        var parent = Transform(earpieceUid).ParentUid;
        if (!parent.IsValid())
            return false;

        return _inventory.TryGetSlotContainer(parent, "ears", out var slot, out _)
               && slot.ContainedEntity == earpieceUid;
    }

    private MapCoordinates GetMapCoordinates(EntityUid uid, TransformComponent xform)
    {
        // If item is in a player's inventory (parent has ActorComponent), use parent's position
        if (xform.ParentUid.IsValid() && TryComp<ActorComponent>(xform.ParentUid, out _))
            return _xform.GetMapCoordinates(xform.ParentUid);
        // Otherwise use the item's own position (handles ground, grid, etc.)
        return _xform.GetMapCoordinates(uid);
    }

    public bool IsJammed(string freq, EntityCoordinates coords)
    {
        var originMap = _xform.ToMapCoordinates(coords);

        var jammers = EntityQueryEnumerator<JammerComponent, TransformComponent>();
        while (jammers.MoveNext(out var uid, out var jammer, out var xform))
        {
            // Unpowered jammer doesn't work
            if (!_power.IsPowered(uid))
                continue;

            var jammerMap = GetMapCoordinates(uid, xform);
            if (jammerMap.MapId != originMap.MapId)
                continue;

            if ((jammerMap.Position - originMap.Position).Length() > jammer.Radius)
                continue;

            if (jammer.JammerType == JammerType.General)
                return true;

            if (jammer.JammerType == JammerType.Specific && jammer.TargetFrequency == freq)
                return true;
        }
        return false;
    }
}
