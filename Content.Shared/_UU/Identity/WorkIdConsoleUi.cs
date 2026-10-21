using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Identity;

/// <summary>Ключи интерфейса консоли работодателя.</summary>
[Serializable, NetSerializable]
public enum WorkIdConsoleUiKey : byte
{
    Console,
    Access,
}

/// <summary>Ключ окна самого удостоверения.</summary>
[Serializable, NetSerializable]
public enum WorkIdCardUiKey : byte
{
    Card,
}

/// <summary>Что клиент видит в окне консоли работодателя.</summary>
[Serializable, NetSerializable]
public sealed class WorkIdConsoleBuiState : BoundUserInterfaceState
{
    public readonly string? Holder;
    public readonly string? Position;
    public readonly string? Employer;
    public readonly string? Date;
    public readonly string? EmployerSignatureLabel;
    public readonly string? Workplace;
    public readonly string? Tag;
    public readonly PhotoSnapshot? Photo;

    public readonly bool HasCard;
    public readonly bool HasPhoto;

    /// <summary>Можно ли менять настройки консоли (доступ).</summary>
    public readonly bool CanEdit;

    /// <summary>Название государства, за которым закреплена консоль.</summary>
    public readonly string? FactionName;

    /// <summary>Теги государства: из них выбирается тег карты.</summary>
    public readonly List<string> Tags;

    public readonly List<string> UseTags;
    public readonly List<string> EditTags;

    public WorkIdConsoleBuiState(
        string? holder,
        string? position,
        string? employer,
        string? date,
        string? employerSignatureLabel,
        string? workplace,
        string? tag,
        PhotoSnapshot? photo,
        bool hasCard,
        bool hasPhoto,
        bool canEdit,
        string? factionName,
        List<string> tags,
        List<string> useTags,
        List<string> editTags)
    {
        Holder = holder;
        Position = position;
        Employer = employer;
        Date = date;
        EmployerSignatureLabel = employerSignatureLabel;
        Workplace = workplace;
        Tag = tag;
        Photo = photo;
        HasCard = hasCard;
        HasPhoto = hasPhoto;
        CanEdit = canEdit;
        FactionName = factionName;
        Tags = tags;
        UseTags = useTags;
        EditTags = editTags;
    }
}

/// <summary>Заполнение карты в консоли.</summary>
[Serializable, NetSerializable]
public sealed class WorkIdConsoleApplyMessage : BoundUserInterfaceMessage
{
    public readonly string Holder;
    public readonly string Position;
    public readonly string Employer;
    public readonly string Workplace;
    public readonly string? Tag;
    public readonly bool ApplyPhoto;

    public WorkIdConsoleApplyMessage(string holder, string position, string employer, string workplace, string? tag, bool applyPhoto)
    {
        Holder = holder;
        Position = position;
        Employer = employer;
        Workplace = workplace;
        Tag = tag;
        ApplyPhoto = applyPhoto;
    }
}

/// <summary>Правка списков доступа консоли.</summary>
[Serializable, NetSerializable]
public sealed class WorkIdConsoleAccessMessage : BoundUserInterfaceMessage
{
    public readonly List<string> UseTags;
    public readonly List<string> EditTags;

    public WorkIdConsoleAccessMessage(List<string> useTags, List<string> editTags)
    {
        UseTags = useTags;
        EditTags = editTags;
    }
}

/// <summary>Состояние окна удостоверения.</summary>
[Serializable, NetSerializable]
public sealed class WorkIdCardBuiState : BoundUserInterfaceState
{
    public readonly string? Holder;
    public readonly string? Position;
    public readonly string? Employer;
    public readonly string? Date;
    public readonly string? EmployerSignatureLabel;
    public readonly string? Workplace;
    public readonly string? Tag;
    public readonly PhotoSnapshot? Photo;

    public WorkIdCardBuiState(string? holder, string? position, string? employer, string? date,
        string? employerSignatureLabel, string? workplace, string? tag, PhotoSnapshot? photo)
    {
        Holder = holder;
        Position = position;
        Employer = employer;
        Date = date;
        EmployerSignatureLabel = employerSignatureLabel;
        Workplace = workplace;
        Tag = tag;
        Photo = photo;
    }
}
