using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._UU.Faction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FactionLeaveDocumentComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? FactionName;

    [DataField, AutoNetworkedField]
    public int? FactionId;

    /// <summary>Текст, написанный на бумаге до подписания (комментарий).</summary>
    [DataField, AutoNetworkedField]
    public string? Comment;
}
