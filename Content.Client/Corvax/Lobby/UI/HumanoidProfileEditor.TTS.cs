using System.Linq;
using Content.Client.Corvax.TTS;
using Content.Shared.Corvax.TTS;
using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<TTSVoicePrototype> _ttsVoices = new();

    /// <summary>
    /// Refills the TTS voice dropdown with voices available for the profile's sex.
    /// </summary>
    public void RefreshVoiceButton()
    {
        TTSVoiceButton.Clear();
        _ttsVoices.Clear();

        if (Profile is null)
            return;

        _ttsVoices = _prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(o => o.RoundStart && HumanoidCharacterProfile.CanHaveVoice(o, Profile.Voice))
            .OrderBy(o => Loc.GetString(o.Name))
            .ToList();

        for (var i = 0; i < _ttsVoices.Count; i++)
        {
            TTSVoiceButton.AddItem(Loc.GetString(_ttsVoices[i].Name), i);
            if (_ttsVoices[i].ID == Profile.TTSVoice)
                TTSVoiceButton.SelectId(i);
        }

        // Current voice is not available for this sex (or no longer exists) - fall back to the first one.
        if (TTSVoiceButton.SelectedId == -1 && _ttsVoices.Count > 0)
        {
            TTSVoiceButton.SelectId(0);
            SetTTSVoice(_ttsVoices[0].ID);
        }
    }

    private void SetTTSVoice(string newVoice)
    {
        Profile = Profile?.WithVoice(newVoice);
        IsDirty = true;
    }

    /// <summary>
    /// Обновляет выпадающий список TTS-голосов (вызывается из слоя внешности при смене пола/вида).
    /// </summary>
    private void UpdateTTSVoiceControls()
    {
        RefreshVoiceButton();
    }
}
