using System.Linq;
using Content.Shared._UU.Radio;
using Content.Server.Chat.Managers;
using Content.Server.Corvax.TTS;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._UU.Radio;

public sealed class RadioChatSystem : EntitySystem
{
    [Dependency] private readonly RadioChannelSystem _radio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly TTSSystem _tts = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UnknownRadioChannelEvent>(OnUnknownChannel);
    }

    private void OnUnknownChannel(UnknownRadioChannelEvent args)
    {
        // Parse :FREQ message format from the original message
        // We handle 1-3 character frequency codes
        var rawMessage = args.OriginalMessage;
        if (!rawMessage.StartsWith(':') || rawMessage.Length < 2)
            return;

        var afterColon = rawMessage.Substring(1).TrimStart();
        if (afterColon.Length == 0)
            return;

        // :* or :all → send on ALL bound frequencies (handled by args.Source)
        if (afterColon.StartsWith("*") || afterColon.StartsWith("все", StringComparison.OrdinalIgnoreCase))
        {
            var message = afterColon.Substring(afterColon[0] == '*' ? 1 : 3).Trim();
            if (string.IsNullOrWhiteSpace(message))
                return;

            // Mark as handled — suppress "No such channel" popup
            args.Handled = true;

            // Collect all bound frequencies
            var allFreqs = new List<string>();
            var radioQuery = EntityQueryEnumerator<RadioComponent, TransformComponent>();
            while (radioQuery.MoveNext(out var uid, out var comp, out var xform))
            {
                if (xform.ParentUid == args.Source)
                    allFreqs.AddRange(comp.Frequencies);
            }
            var earpieceQuery = EntityQueryEnumerator<EarpieceComponent, TransformComponent>();
            while (earpieceQuery.MoveNext(out var uid, out var comp, out var xform))
            {
                if (xform.ParentUid == args.Source)
                    allFreqs.AddRange(comp.Frequencies);
            }

            foreach (var f in allFreqs.Distinct())
                BroadcastOnFrequency(f, args.Source, message);
            return;
        }

        // Try to extract a frequency code: 1-3 alphanumeric chars after :
        int freqLen = 0;
        for (int i = 0; i < afterColon.Length && i < 3; i++)
        {
            if (char.IsLetterOrDigit(afterColon[i]))
                freqLen++;
            else
                break;
        }

        if (freqLen == 0)
            return;

        var freq = afterColon.Substring(0, freqLen).ToUpper();
        var msg = afterColon.Substring(freqLen).Trim();

        if (string.IsNullOrWhiteSpace(msg))
            return;

        // Mark as handled — suppress "No such channel" popup
        args.Handled = true;

        BroadcastOnFrequency(freq, args.Source, msg);
    }

    /// <summary>
    /// Sending via :FREQ chat — sender must own an active, transmit-enabled device on this freq.
    /// </summary>
    private void BroadcastOnFrequency(string freq, EntityUid sender, string message)
    {
        var sourceCoords = Transform(sender).Coordinates;
        var sourceMap = _xform.GetMapCoordinates(sender);

        if (_radio.IsJammed(freq, sourceCoords))
        {
            _popup.PopupEntity(Loc.GetString("radio-jammed"), sender, sender);
            return;
        }

        // Check if sender is inside the coverage zone (transmitter or towers)
        if (!_radio.IsPointCovered(freq, sourceMap))
        {
            // No coverage — frequency is dead here, silently drop
            return;
        }

        // The sender must actually have an active device bound to this frequency to talk
        if (!SenderHasFrequency(sender, freq))
            return; // silently drop

        // Mark so the auto-whisper from the chat system isn't re-relayed as local speech,
        // and its TTS reaches only listeners without the frequency.
        _radio.MarkBroadcast(sender, freq);

        SendToFrequency(freq, sender, message, ttsIncludeSource: true);
    }

    /// <summary>
    /// Relays local speech: an active radio within 1 tile of the speaker picks it up
    /// and transmits it on its bound frequencies.
    /// </summary>
    public void RelayLocalSpeech(string freq, EntityUid speaker, string message)
    {
        var sourceCoords = Transform(speaker).Coordinates;
        var sourceMap = _xform.GetMapCoordinates(speaker);

        if (_radio.IsJammed(freq, sourceCoords))
            return;

        if (!_radio.IsPointCovered(freq, sourceMap))
            return;

        SendToFrequency(freq, speaker, message);
    }

    private void SendToFrequency(string freq, EntityUid source, string message, bool ttsIncludeSource = false)
    {
        var sourceMap = _xform.GetMapCoordinates(source);
        var name = MetaData(source).EntityName;
        var freqName = FormattedMessage.EscapeText(_radio.GetFrequencyName(freq) ?? freq);
        var wrappedMessage = $"[color=#ff8800]\\[{freqName}\\] [bold]{FormattedMessage.EscapeText(name)}[/bold]: {FormattedMessage.EscapeText(message)}[/color]";

        // Collect eligible recipients
        var recipients = new List<ICommonSession>();
        ICommonSession? sourceSession = null;

        // Always include the source — this is a frequency broadcast, not a player network
        if (TryComp(source, out ActorComponent? sourceActor))
        {
            sourceSession = sourceActor.PlayerSession;
            recipients.Add(sourceSession);
        }

        foreach (var (uid, coords) in _radio.GetReceiversOnFrequency(freq, Transform(source).Coordinates))
        {
            if (coords.MapId != sourceMap.MapId)
                continue;

            // Receiver must be inside the coverage zone (transmitter or towers)
            if (!_radio.IsPointCovered(freq, coords))
                continue;

            var receiverCoords = Transform(uid).Coordinates;
            if (_radio.IsJammed(freq, receiverCoords))
                continue;

            var parent = Transform(uid).ParentUid;
            if (TryComp(parent, out ActorComponent? actor))
                recipients.Add(actor.PlayerSession);
            else if (TryComp(uid, out ActorComponent? directActor))
                recipients.Add(directActor.PlayerSession);
        }

        // Send to all eligible recipients
        var distinct = recipients.Distinct().ToList();
        foreach (var session in distinct)
            _chatMan.ChatMessageToOne(ChatChannel.Radio, message, wrappedMessage, source, false, session.Channel);

        // Voice the message for radio listeners. For direct :FREQ broadcasts the source
        // hears their own transmission through the radio too (even with no other listeners);
        // for local speech relay the source already hears themselves speaking normally.
        var ttsRecipients = ttsIncludeSource
            ? distinct
            : distinct.Where(s => s != sourceSession).ToList();
        if (ttsRecipients.Count > 0)
            _ = _tts.GenerateRadioTTS(source, message, ttsRecipients);
    }

    private bool SenderHasFrequency(EntityUid owner, string freq)
    {
        var radioQuery = EntityQueryEnumerator<RadioComponent, TransformComponent>();
        while (radioQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Frequencies.Contains(freq) || !comp.TransmitEnabled)
                continue;
            if (BelongsTo(uid, owner) && _radio.IsRadioActive(uid))
                return true;
        }

        var earpieceQuery = EntityQueryEnumerator<EarpieceComponent, TransformComponent>();
        while (earpieceQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            // Гарнитура даёт право передачи только когда надета в слот ушей,
            // иначе достаточно было бы носить её в рюкзаке.
            if (comp.Frequencies.Contains(freq) && _radio.IsEarpieceActive(uid) && BelongsTo(uid, owner))
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
}
