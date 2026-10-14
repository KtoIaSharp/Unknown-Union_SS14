using Content.Shared._UU.Radio;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server._UU.Radio;

public sealed class JammerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JammerComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<JammerComponent, FrequencySetMessage>(OnFrequencySet);
    }

    private void OnBuiOpened(Entity<JammerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not FrequencyUiKey.Jammer)
            return;

        UpdateBui(ent);
    }

    private void OnFrequencySet(Entity<JammerComponent> ent, ref FrequencySetMessage args)
    {
        var freq = args.Frequency.Trim();
        if (string.IsNullOrWhiteSpace(freq) || freq.Length > 3)
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
        var state = new FrequencyBuiState(ent.Comp.TargetFrequency, new());
        _ui.SetUiState(ent.Owner, FrequencyUiKey.Jammer, state);
    }
}
