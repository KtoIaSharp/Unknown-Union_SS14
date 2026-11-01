using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Identity;

/// <summary>
///     Запись реестра государства: кто, на какой должности, с каким тегом и кто назначил.
///     Запись создаётся сканером должности и живёт, пока государство существует.
/// </summary>
[DataDefinition, Serializable]
public sealed partial class WorkRegistryEntry
{
    /// <summary>Имя работника.</summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>Должность.</summary>
    [DataField]
    public string Position = string.Empty;

    /// <summary>Рабочий тег, который выдают вместе с должностью.</summary>
    [DataField]
    public string? Tag;

    /// <summary>Кто назначил (имя или название государства).</summary>
    [DataField]
    public string? Employer;

    /// <summary>Дата оформления.</summary>
    [DataField]
    public string? Date;

    /// <summary>Человек уволен с этой должности: тег с этой связкой больше не действует.</summary>
    [DataField]
    public bool Revoked;
}

/// <summary>Ключи интерфейса консоли управления должностями.</summary>
[Serializable, NetSerializable]
public enum WorkRegistryUiKey : byte
{
    Registry,
    Access,
}

/// <summary>
///     Консоль управления должностями: показывает реестр государства и даёт увольнять.
///     Сама консоль закрепляется за государством и реестр общий для всех его консолей.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WorkRegistryConsoleComponent : Component
{
    /// <summary>Государство, за которым закреплена консоль.</summary>
    [DataField, AutoNetworkedField]
    public int? FactionId;

    [DataField, AutoNetworkedField]
    public string? FactionName;

    /// <summary>Теги, носители которых могут пользоваться консолью. Пусто — все.</summary>
    [DataField, AutoNetworkedField]
    public List<string> UseTags = new();

    /// <summary>Теги, носители которых могут менять настройки. Пусто — как <see cref="UseTags"/>.</summary>
    [DataField, AutoNetworkedField]
    public List<string> EditTags = new();
}

/// <summary>
///     Сканер должности: им сканируют удостоверение, чтобы внести должность и тег
///     в реестр государства, за которым закреплён сканер.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PositionScannerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? FactionId;

    [DataField, AutoNetworkedField]
    public string? FactionName;
}

/// <summary>Что клиент видит в окне консоли управления должностями.</summary>
[Serializable, NetSerializable]
public sealed class WorkRegistryBuiState : BoundUserInterfaceState
{
    public readonly string? FactionName;
    public readonly List<WorkRegistryEntry> Entries;
    public readonly List<string> Tags;
    public readonly List<string> UseTags;
    public readonly List<string> EditTags;
    public readonly bool CanUse;
    public readonly bool CanEdit;
    public readonly bool Powered;

    public WorkRegistryBuiState(
        string? factionName,
        List<WorkRegistryEntry> entries,
        List<string> tags,
        List<string> useTags,
        List<string> editTags,
        bool canUse,
        bool canEdit,
        bool powered)
    {
        FactionName = factionName;
        Entries = entries;
        Tags = tags;
        UseTags = useTags;
        EditTags = editTags;
        CanUse = canUse;
        CanEdit = canEdit;
        Powered = powered;
    }
}

/// <summary>Увольнение или восстановление работника.</summary>
[Serializable, NetSerializable]
public sealed class WorkRegistryRevokeMessage : BoundUserInterfaceMessage
{
    public readonly string Name;
    public readonly string Position;
    public readonly string Tag;
    public readonly bool Revoke;

    public WorkRegistryRevokeMessage(string name, string position, string tag, bool revoke)
    {
        Name = name;
        Position = position;
        Tag = tag;
        Revoke = revoke;
    }
}

/// <summary>Правка списков доступа консоли.</summary>
[Serializable, NetSerializable]
public sealed class WorkRegistryAccessMessage : BoundUserInterfaceMessage
{
    public readonly List<string> UseTags;
    public readonly List<string> EditTags;

    public WorkRegistryAccessMessage(List<string> useTags, List<string> editTags)
    {
        UseTags = useTags;
        EditTags = editTags;
    }
}
