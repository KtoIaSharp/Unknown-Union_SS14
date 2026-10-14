using System.Linq;
using Content.Client.Corvax.TTS;
using Content.Shared.Corvax.TTS;
using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<TTSVoicePrototype> _voices = new();

    /// <summary>
    /// Refills the voice dropdown with voices available for the profile's sex.
    /// </summary>
    public void RefreshVoiceButton()
    {
        VoiceButton.Clear();
        _voices.Clear();

        if (Profile is null)
            return;

        _voices = _prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(o => o.RoundStart && HumanoidCharacterProfile.CanHaveVoice(o, Profile.Sex))
            .OrderBy(o => Loc.GetString(o.Name))
            .ToList();

        for (var i = 0; i < _voices.Count; i++)
        {
            VoiceButton.AddItem(Loc.GetString(_voices[i].Name), i);
            if (_voices[i].ID == Profile.Voice)
                VoiceButton.SelectId(i);
        }

        // Current voice is not available for this sex (or no longer exists) - fall back to the first one.
        if (VoiceButton.SelectedId == -1 && _voices.Count > 0)
        {
            VoiceButton.SelectId(0);
            SetVoice(_voices[0].ID);
        }
    }

    private void SetVoice(string newVoice)
    {
        Profile = Profile?.WithVoice(newVoice);
        IsDirty = true;
    }
}
