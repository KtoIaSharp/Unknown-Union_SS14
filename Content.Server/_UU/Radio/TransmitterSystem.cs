using System.Linq;
using Content.Shared._UU.Radio;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server._UU.Radio;

public sealed class TransmitterSystem : EntitySystem
{
    [Dependency] private readonly RadioChannelSystem _radio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransmitterComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<TransmitterComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<TransmitterComponent, FrequencySetMessage>(OnFrequencySet);
        SubscribeLocalEvent<TransmitterComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnActivate(Entity<TransmitterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex || args.Handled)
            return;

        // Configured transmitter cannot be opened — no menu at all
        if (ent.Comp.Configured)
            return;

        args.Handled = true;
        _ui.OpenUi(ent.Owner, FrequencyUiKey.Transmitter, args.User);
    }

    private void OnBuiOpened(Entity<TransmitterComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not FrequencyUiKey.Transmitter)
            return;

        // If the frequency is already configured — the menu must disappear
        if (ent.Comp.Configured)
        {
            _ui.CloseUi(ent.Owner, FrequencyUiKey.Transmitter, args.Actor);
            return;
        }

        UpdateBui(ent);
    }

    private void OnFrequencySet(Entity<TransmitterComponent> ent, ref FrequencySetMessage args)
    {
        // Can only configure once
        if (ent.Comp.Configured)
        {
            _popup.PopupEntity(Loc.GetString("radio-already-configured"), ent.Owner, ent.Owner);
            return;
        }

        var freq = args.Frequency.Trim();

        // Только буквы и цифры: частота попадает в чат и интерфейс,
        // произвольные символы позволяли бы ломать разметку сообщений.
        if (string.IsNullOrWhiteSpace(freq) || freq.Length > 3 || !freq.All(char.IsLetterOrDigit))
        {
            _popup.PopupEntity(Loc.GetString("radio-invalid-frequency"), ent.Owner, ent.Owner);
            return;
        }

        ent.Comp.Frequency = freq;
        ent.Comp.FrequencyName = args.Name;
        ent.Comp.Configured = true;
        _radio.TryRegisterFrequency(freq, args.Name);
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("radio-set-success"), args.Actor, args.Actor);
        _ui.CloseUi(ent.Owner, FrequencyUiKey.Transmitter, args.Actor);
    }

    private void OnShutdown(Entity<TransmitterComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Frequency != null)
            _radio.TryUnregisterFrequency(ent.Comp.Frequency);
    }

    private void UpdateBui(Entity<TransmitterComponent> ent)
    {
        var state = new FrequencyBuiState(
            ent.Comp.Frequency,
            ent.Comp.FrequencyName,
            configured: ent.Comp.Configured);
        _ui.SetUiState(ent.Owner, FrequencyUiKey.Transmitter, state);
    }
}
