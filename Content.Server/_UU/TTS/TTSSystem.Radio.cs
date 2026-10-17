using System.Threading.Tasks;
using Content.Shared.Corvax.TTS;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    /// <summary>
    /// Озвучивает радиосообщение для конкретного списка слушателей (система частот UU).
    /// Звук проигрывается как нелокализованный, поэтому слушатели могут быть вне PVS источника.
    /// </summary>
    public async Task<bool> GenerateRadioTTS(EntityUid speaker, string message, IEnumerable<ICommonSession> recipients)
    {
        if (!_isEnabled || message.Length > MaxMessageChars)
            return false;

        if (!TryComp<TTSComponent>(speaker, out var component) || string.IsNullOrEmpty(component.VoicePrototypeId))
            return false;

        var voiceId = component.VoicePrototypeId;
        var voiceEv = new TransformSpeakerVoiceEvent(speaker, voiceId);
        RaiseLocalEvent(speaker, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!ProtoMan.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return false;

        var soundData = await GenerateTTS(message, protoVoice.Speaker);
        if (soundData is null || TerminatingOrDeleted(speaker))
            return false;

        var ttsEvent = new PlayTTSEvent(soundData, GetNetEntity(speaker), false, true);
        foreach (var session in recipients)
        {
            RaiseNetworkEvent(ttsEvent, session);
        }

        return true;
    }
}
