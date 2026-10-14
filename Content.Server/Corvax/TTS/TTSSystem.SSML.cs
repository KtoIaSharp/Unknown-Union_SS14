namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    private string ToSsmlText(string text, SoundTraits traits = SoundTraits.None)
    {
        var result = text;
        // Silero v4 applies rate="fast" much more aggressively than v3 - speech becomes
        // rushed. "medium" gives the natural pace that v3's "fast" used to produce.
        if (traits.HasFlag(SoundTraits.RateFast))
            result = $"<prosody rate=\"medium\">{result}</prosody>";
        if (traits.HasFlag(SoundTraits.PitchVerylow))
            result = $"<prosody pitch=\"x-low\">{result}</prosody>";
        return $"<speak>{result}</speak>";
    }
    
    [Flags]
    private enum SoundTraits : ushort
    {
        None = 0,
        RateFast = 1 << 0,
        PitchVerylow = 1 << 1,
    }
}
