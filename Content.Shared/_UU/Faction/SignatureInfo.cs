using Robust.Shared.Serialization;

namespace Content.Shared._UU.Faction;

[DataDefinition, Serializable, NetSerializable]
public partial struct SignatureInfo
{
    [DataField("name")]
    public string Name;

    [DataField("date")]
    public string Date;

    /// <summary>Ключ локализации для глагола (подписал/подписала/подписало/подписано от имени).</summary>
    [DataField("signKey")]
    public string SignKey;

    /// <summary>ID спрайта подписи из RSI (sig01, sig02, ...).</summary>
    [DataField("sprite")]
    public string Sprite;
}
