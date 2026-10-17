using Content.Shared._UU.Radio;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server._UU.Radio;

/// <summary>
/// Handles the frequency configurator via direct interactions (ПКМ):
/// - click a transmitter to copy its frequency (+ name)
/// - click a radio/earpiece to paste
/// - click a specific jammer or signal tower to paste
/// </summary>
public sealed class FrequencyConfiguratorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrequencyConfiguratorComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<FrequencyConfiguratorComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        var target = args.Target.Value;

        // Copy from transmitter
        if (TryComp<TransmitterComponent>(target, out var transmitter) && transmitter.Frequency != null)
        {
            ent.Comp.StoredFrequency = transmitter.Frequency;
            ent.Comp.StoredFrequencyName = transmitter.FrequencyName;
            Dirty(ent);
            _popup.PopupEntity(Loc.GetString("radio-configurator-copied", ("frequency", (object)transmitter.Frequency)),
                args.User, args.User);
            args.Handled = true;
            return;
        }

        var freq = ent.Comp.StoredFrequency;
        if (freq == null)
        {
            _popup.PopupEntity(Loc.GetString("radio-configurator-empty"), args.User, args.User);
            return;
        }

        // Paste onto radio
        if (TryComp<RadioComponent>(target, out var radio))
        {
            if (!radio.Frequencies.Contains(freq))
            {
                radio.Frequencies.Add(freq);
                Dirty(target, radio);
                _popup.PopupEntity(Loc.GetString("radio-configurator-pasted", ("frequency", (object)freq)),
                    args.User, args.User);
                args.Handled = true;
            }
            return;
        }

        // Paste onto earpiece
        if (TryComp<EarpieceComponent>(target, out var earpiece))
        {
            if (!earpiece.Frequencies.Contains(freq))
            {
                earpiece.Frequencies.Add(freq);
                Dirty(target, earpiece);
                _popup.PopupEntity(Loc.GetString("radio-configurator-pasted", ("frequency", (object)freq)),
                    args.User, args.User);
                args.Handled = true;
            }
            return;
        }

        // Paste onto specific jammer (sets its target frequency)
        if (TryComp<JammerComponent>(target, out var jammer) && jammer.JammerType == JammerType.Specific)
        {
            jammer.TargetFrequency = freq;
            Dirty(target, jammer);
            _popup.PopupEntity(Loc.GetString("radio-configurator-pasted", ("frequency", (object)freq)),
                args.User, args.User);
            args.Handled = true;
            return;
        }

        // Paste onto signal tower (sets its frequency)
        if (TryComp<SignalTowerComponent>(target, out var tower))
        {
            // Вышка под кодом: конфигуратор — не отмычка, нужен код.
            if (TryComp<SignalTowerAccessComponent>(target, out var access)
                && !string.IsNullOrEmpty(access.Code))
            {
                _popup.PopupEntity(Loc.GetString("tower-locked"), args.User, args.User);
                args.Handled = true;
                return;
            }

            tower.Frequency = freq;
            Dirty(target, tower);
            _popup.PopupEntity(Loc.GetString("radio-configurator-pasted", ("frequency", (object)freq)),
                args.User, args.User);
            args.Handled = true;
        }
    }
}
