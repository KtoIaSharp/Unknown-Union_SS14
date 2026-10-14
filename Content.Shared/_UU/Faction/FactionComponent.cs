using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared._UU.Faction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FactionComponent : Component
{
    /// <summary>Название государства (для отображения).</summary>
    [DataField, AutoNetworkedField]
    public string? Faction;

    /// <summary>Уникальный ID государства (для всех операций).</summary>
    [DataField, AutoNetworkedField]
    public int? FactionId;

    [DataField, AutoNetworkedField]
    public string? Title;

    [DataField, AutoNetworkedField]
    public bool IsLeader;

    /// <summary>Зафиксированный на этот раунд спрайт подписи (sig01-sig10).</summary>
    [DataField, AutoNetworkedField]
    public string? SignatureSprite;
}
