using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Employment;

/// <summary>
///     Ключ интерфейса договора о трудоустройстве.
/// </summary>
[Serializable, NetSerializable]
public enum EmploymentContractUiKey : byte
{
    Contract,
}

/// <summary>
///     Что клиент видит в окне договора. Подписи передаются как метка (имя человека
///     или название государства) и спрайт росчерка.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmploymentContractBuiState : BoundUserInterfaceState
{
    public readonly string? Worker;
    public readonly string? Position;
    public readonly string? Employer;
    public readonly string? Date;
    public readonly string? EmployerSignature;
    public readonly string? EmployerSignatureLabel;
    public readonly string? WorkerSignature;
    public readonly string? WorkerSignatureLabel;

    /// <summary>Игрок — глава государства и может подписать договор от его имени.</summary>
    public readonly bool CanSignAsState;

    /// <summary>Название государства подписанта, если он вообще в нём состоит.</summary>
    public readonly string? StateName;

    /// <summary>Текст самого бланка: договор — это обычный лист бумаги со своим содержимым.</summary>
    public readonly string Body;

    public EmploymentContractBuiState(
        string? worker,
        string? position,
        string? employer,
        string? date,
        string? employerSignature,
        string? employerSignatureLabel,
        string? workerSignature,
        string? workerSignatureLabel,
        bool canSignAsState,
        string? stateName,
        string body)
    {
        Worker = worker;
        Position = position;
        Employer = employer;
        Date = date;
        EmployerSignature = employerSignature;
        EmployerSignatureLabel = employerSignatureLabel;
        WorkerSignature = workerSignature;
        WorkerSignatureLabel = workerSignatureLabel;
        CanSignAsState = canSignAsState;
        StateName = stateName;
        Body = body;
    }
}

/// <summary>
///     Заполнение полей договора: кого назначают, кто назначает и на какую должность.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmploymentContractFieldsMessage : BoundUserInterfaceMessage
{
    public readonly string Worker;
    public readonly string Employer;
    public readonly string Position;

    public EmploymentContractFieldsMessage(string worker, string employer, string position)
    {
        Worker = worker;
        Employer = employer;
        Position = position;
    }
}

/// <summary>
///     Подпись договора: отдельно от работодателя и отдельно от работника;
///     подпись от имени государства — только для главы государства.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmploymentContractSignMessage : BoundUserInterfaceMessage
{
    public readonly bool AsEmployer;
    public readonly bool AsState;

    public EmploymentContractSignMessage(bool asEmployer, bool asState)
    {
        AsEmployer = asEmployer;
        AsState = asState;
    }
}
