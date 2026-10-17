using Content.Shared._UU.Radio;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server._UU.Radio;

/// <summary>
/// Глушители не имеют своего интерфейса: частоту ставит только конфигуратор частот
/// (см. <see cref="FrequencyConfiguratorSystem"/>). Здесь — только обратная связь при осмотре.
/// </summary>
public sealed class JammerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JammerComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(Entity<JammerComponent> ent, ref ActivateInWorldEvent args)
    {
        // Общий глушитель ничего не настраивает — ему незачем знать частоту.
        if (ent.Comp.JammerType != JammerType.Specific)
            return;

        var text = ent.Comp.TargetFrequency == null
            ? Loc.GetString("radio-jammer-no-target")
            : Loc.GetString("radio-jammer-target", ("frequency", (object) ent.Comp.TargetFrequency));

        _popup.PopupEntity(text, ent.Owner, args.User);
    }
}
