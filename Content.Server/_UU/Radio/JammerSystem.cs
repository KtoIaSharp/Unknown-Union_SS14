using System.Linq;
using Content.Shared._UU.Radio;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._UU.Radio;

public sealed class JammerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Кулдаун на смену частоты: попап рисуется всем рядом с глушителем, спам им не нужен.
    private static readonly TimeSpan SetCooldown = TimeSpan.FromSeconds(1);
    private readonly Dictionary<EntityUid, TimeSpan> _lastSet = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JammerComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<JammerComponent, FrequencySetMessage>(OnFrequencySet);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _lastSet.Clear();
    }

    private void OnBuiOpened(Entity<JammerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not FrequencyUiKey.Jammer)
            return;

        UpdateBui(ent);
    }

    private void OnFrequencySet(Entity<JammerComponent> ent, ref FrequencySetMessage args)
    {
        // Частоту задаёт только избирательный глушитель — у общего нет BUI и целевой частоты.
        if (ent.Comp.JammerType != JammerType.Specific)
            return;

        if (args.Actor is { } actor)
        {
            var now = _timing.CurTime;
            if (_lastSet.TryGetValue(actor, out var lastSet) && now - lastSet < SetCooldown)
                return;
            _lastSet[actor] = now;
        }

        var freq = args.Frequency.Trim();
        if (string.IsNullOrWhiteSpace(freq) || freq.Length > 3 || !freq.All(char.IsLetterOrDigit))
        {
            _popup.PopupEntity(Loc.GetString("radio-invalid-frequency"), ent.Owner, ent.Owner);
            return;
        }

        ent.Comp.TargetFrequency = freq;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("radio-jammer-frequency-set", ("frequency", (object)freq)), ent.Owner, ent.Owner);
        UpdateBui(ent);
    }

    private void UpdateBui(Entity<JammerComponent> ent)
    {
        var state = new FrequencyBuiState(ent.Comp.TargetFrequency);
        _ui.SetUiState(ent.Owner, FrequencyUiKey.Jammer, state);
    }
}
