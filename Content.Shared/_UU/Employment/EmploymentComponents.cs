using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._UU.Employment;

/// <summary>
///     Договор о трудоустройстве: кого назначают, кто назначает, должность и дата,
///     плюс две подписи — отдельно от работодателя и отдельно от работника.
///     Пока обеих подписей нет, договор считается незаключённым.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmploymentContractComponent : Component
{
    /// <summary>Максимальная длина текстового поля договора.</summary>
    public const int MaxFieldLength = 48;

    /// <summary>Кого назначают (имя работника).</summary>
    [DataField, AutoNetworkedField]
    public string? Worker;

    /// <summary>Должность, на которую назначают.</summary>
    [DataField, AutoNetworkedField]
    public string? Position;

    /// <summary>Кто назначает (имя работодателя).</summary>
    [DataField, AutoNetworkedField]
    public string? Employer;

    /// <summary>Дата заключения договора.</summary>
    [DataField, AutoNetworkedField]
    public string? Date;

    /// <summary>Спрайт подписи работодателя из signatures.rsi.</summary>
    [DataField, AutoNetworkedField]
    public string? EmployerSignature;

    /// <summary>Чья это подпись: имя человека или название государства.</summary>
    [DataField, AutoNetworkedField]
    public string? EmployerSignatureLabel;

    /// <summary>Спрайт подписи работника.</summary>
    [DataField, AutoNetworkedField]
    public string? WorkerSignature;

    /// <summary>Чья это подпись: имя человека или название государства.</summary>
    [DataField, AutoNetworkedField]
    public string? WorkerSignatureLabel;

    /// <summary>Обе подписи на месте — с договора можно считывать данные.</summary>
    [ViewVariables]
    public bool Complete =>
        !string.IsNullOrWhiteSpace(EmployerSignature) &&
        !string.IsNullOrWhiteSpace(WorkerSignature);
}

/// <summary>
///     Удостоверение о работе: пластиковая карта, куда «ручной работодатель»
///     переносит данные с подписанного договора. Сами данные на карте не редактируются.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WorkIdCardComponent : Component
{
    /// <summary>Имя владельца карты.</summary>
    [DataField, AutoNetworkedField]
    public string? Holder;

    /// <summary>Должность.</summary>
    [DataField, AutoNetworkedField]
    public string? Position;

    /// <summary>Работодатель, подписавший договор.</summary>
    [DataField, AutoNetworkedField]
    public string? Employer;

    /// <summary>Дата оформления.</summary>
    [DataField, AutoNetworkedField]
    public string? Date;

    /// <summary>Спрайт подписи работодателя на карте.</summary>
    [DataField, AutoNetworkedField]
    public string? EmployerSignature;

    /// <summary>Чья подпись: имя человека или название государства.</summary>
    [DataField, AutoNetworkedField]
    public string? EmployerSignatureLabel;

    /// <summary>Данные перенесены — карта выдана.</summary>
    [DataField, AutoNetworkedField]
    public bool Issued;
}

/// <summary>
///     Ручной работодатель: сканер-записыватель, который переписывает данные
///     с подписанного договора на пластиковое удостоверение.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WorkIdWriterComponent : Component
{
}
