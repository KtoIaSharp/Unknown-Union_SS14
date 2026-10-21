using System.Collections.Generic;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared._UU.Identity;

/// <summary>
///     Консоль работодателя: вставляет карту и фотографию, заполняет поля,
///     хранит списки тегов доступа своего государства.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WorkIdConsoleComponent : Component
{
    public const string CardSlotId = "card";
    public const string PhotoSlotId = "photo";

    [DataField]
    public ItemSlot CardSlot = new();

    [DataField]
    public ItemSlot PhotoSlot = new();

    /// <summary>Государство, за которым закреплена консоль.</summary>
    [DataField, AutoNetworkedField]
    public int? FactionId;

    [DataField, AutoNetworkedField]
    public string? FactionName;

    /// <summary>Кто может пользоваться консолью (по тегу карты). Пусто — все.</summary>
    [DataField, AutoNetworkedField]
    public List<string> UseTags = new();

    /// <summary>Кто может менять настройки консоли. Пусто — как <see cref="UseTags"/>.</summary>
    [DataField, AutoNetworkedField]
    public List<string> EditTags = new();
}
