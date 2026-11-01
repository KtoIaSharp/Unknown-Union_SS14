using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Identity;

/// <summary>Ключ окна удостоверения о работе.</summary>
[Serializable, NetSerializable]
public enum WorkIdCardUiKey : byte
{
    Card,
}

/// <summary>Всё, что видно на удостоверении: поля, подписи и фотография.</summary>
[Serializable, NetSerializable]
public sealed class WorkIdCardBuiState : BoundUserInterfaceState
{
    public readonly string? Holder;
    public readonly string? Position;
    public readonly string? Employer;
    public readonly string? Date;
    public readonly string? Tag;

    public readonly string? EmployerSignature;
    public readonly string? EmployerSignatureLabel;
    public readonly string? WorkerSignature;
    public readonly string? WorkerSignatureLabel;

    public readonly PhotoSnapshot? Photo;

    /// <summary>Тег отозван консолью государства: карта больше не применяет свои права.</summary>
    public readonly bool Revoked;

    /// <summary>Государство, отозвавшее тег.</summary>
    public readonly string? RevokedBy;

    /// <summary>Дата отзыва.</summary>
    public readonly string? RevokedAt;

    /// <summary>Стоит ли на карте печать реестра.</summary>
    public readonly bool RegistryStamped;

    /// <summary>Государство, чья печать стоит на карте (null — без привязки).</summary>
    public readonly string? RegistryStampFaction;

    public WorkIdCardBuiState(
        string? holder,
        string? position,
        string? employer,
        string? date,
        string? tag,
        string? employerSignature,
        string? employerSignatureLabel,
        string? workerSignature,
        string? workerSignatureLabel,
        PhotoSnapshot? photo,
        bool revoked,
        string? revokedBy,
        string? revokedAt,
        bool registryStamped,
        string? registryStampFaction)
    {
        Holder = holder;
        Position = position;
        Employer = employer;
        Date = date;
        Tag = tag;
        EmployerSignature = employerSignature;
        EmployerSignatureLabel = employerSignatureLabel;
        WorkerSignature = workerSignature;
        WorkerSignatureLabel = workerSignatureLabel;
        Photo = photo;
        Revoked = revoked;
        RevokedBy = revokedBy;
        RevokedAt = revokedAt;
        RegistryStamped = registryStamped;
        RegistryStampFaction = registryStampFaction;
    }
}
