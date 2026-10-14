using Content.Shared._UU.Radio;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Server._UU.Radio;

public sealed class RadioSystem : EntitySystem
{
    [Dependency] private readonly RadioChannelSystem _radio = default!;
    [Dependency] private readonly RadioChatSystem _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioComponent, AfterInteractEvent>(OnRadioInteract);
        SubscribeLocalEvent<RadioComponent, UseInHandEvent>(OnRadioUseInHand);
        SubscribeLocalEvent<EarpieceComponent, AfterInteractEvent>(OnEarpieceInteract);
        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke);
    }

    // Z — toggle transmit on/off
    private void OnRadioUseInHand(Entity<RadioComponent> ent, ref UseInHandEvent args)
    {
        ent.Comp.TransmitEnabled = !ent.Comp.TransmitEnabled;
        Dirty(ent);
        _popup.PopupEntity(
            Loc.GetString(ent.Comp.TransmitEnabled ? "radio-transmit-on" : "radio-transmit-off"),
            args.User, args.User);
        args.Handled = true;
    }

    // Local speech within 1 tile of an active radio gets relayed on its frequencies.
    // Whispers that are just the auto-whisper after a radio send are skipped (no double send).
    private void OnEntitySpoke(EntitySpokeEvent args)
    {
        // Skip the auto-whisper created right after a :FREQ broadcast
        if (args.ObfuscatedMessage != null && _radio.WasRecentlyBroadcast(args.Source, out _))
            return;

        var speakerMap = _xform.GetMapCoordinates(args.Source);

        var radioQuery = EntityQueryEnumerator<RadioComponent, TransformComponent>();
        while (radioQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.TransmitEnabled || comp.Frequencies.Count == 0)
                continue;

            if (!_radio.IsRadioActive(uid))
                continue;

            var radioMap = _radio.GetMapCoordinates(uid);
            if (radioMap.MapId != speakerMap.MapId)
                continue;

            if ((radioMap.Position - speakerMap.Position).Length() > 1f)
                continue;

            foreach (var freq in comp.Frequencies)
                _chat.RelayLocalSpeech(freq, args.Source, args.Message);
        }
    }

    // Bind radio to a transmitter via direct interaction (ПКМ on transmitter with radio in hand)
    private void OnRadioInteract(Entity<RadioComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!TryComp<TransmitterComponent>(args.Target.Value, out var transmitter) || transmitter.Frequency == null)
            return;

        var freq = transmitter.Frequency;
        if (!ent.Comp.Frequencies.Contains(freq))
        {
            ent.Comp.Frequencies.Add(freq);
            Dirty(ent);
            _popup.PopupEntity(Loc.GetString("radio-bound", ("frequency", (object)freq)), args.User, args.User);
        }
        else
            _popup.PopupEntity(Loc.GetString("radio-already-bound", ("frequency", (object)freq)), args.User, args.User);

        args.Handled = true;
    }

    // Bind earpiece to a transmitter via direct interaction
    private void OnEarpieceInteract(Entity<EarpieceComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (!TryComp<TransmitterComponent>(args.Target.Value, out var transmitter) || transmitter.Frequency == null)
            return;

        var freq = transmitter.Frequency;
        if (!ent.Comp.Frequencies.Contains(freq))
        {
            ent.Comp.Frequencies.Add(freq);
            Dirty(ent);
            _popup.PopupEntity(Loc.GetString("radio-bound", ("frequency", (object)freq)), args.User, args.User);
        }
        else
            _popup.PopupEntity(Loc.GetString("radio-already-bound", ("frequency", (object)freq)), args.User, args.User);

        args.Handled = true;
    }
}
