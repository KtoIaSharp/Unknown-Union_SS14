using System;
using System.Collections.Generic;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Identity;

/// <summary>
///     «Слепок» человека для фотографии: только то, что относится к телу и базовой одежде.
///     Кепки, маски, очки и прочие аксессуары сюда не попадают — как на паспортное фото.
///     Сам портрет пока не рисуется (поле <see cref="Portrait"/> — заглушка под будущий рендер).
/// </summary>
[DataDefinition, Serializable]
public sealed partial class PhotoSnapshot
{
    /// <summary>Чьё это фото.</summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>Раса.</summary>
    [DataField]
    public ProtoId<SpeciesPrototype> Species = "Human";

    [DataField]
    public Sex Sex;

    [DataField]
    public Gender Gender;

    [DataField]
    public int Age = 18;

    /// <summary>Причёска: прототип причёски.</summary>
    [DataField]
    public string? HairStyle;

    [DataField]
    public Color HairColor = Color.White;

    /// <summary>Цвет кожи — по нему рисуется лицо на портрете.</summary>
    [DataField]
    public Color SkinColor = Color.FromHex("#E0B58A");

    /// <summary>Борода или усы: прототип.</summary>
    [DataField]
    public string? FacialHairStyle;

    [DataField]
    public Color FacialHairColor = Color.White;

    /// <summary>Базовая одежда: комбинезон и верхняя одежда (прототипы).</summary>
    [DataField]
    public List<string> Clothing = new();

    /// <summary>Заглушка под готовый отрендеренный портрет (спрайт/состояние).</summary>
    [DataField]
    public string? Portrait;
}

/// <summary>
///     Фотография как предмет: её снимает фотоаппарат и её можно вставить в консоль,
///     чтобы перенести слепок на удостоверение.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WorkPhotoComponent : Component
{
    [DataField, AutoNetworkedField]
    public PhotoSnapshot? Snapshot;
}

/// <summary>
///     Фотоаппарат: клик по человеку делает снимок и создаёт предмет-фотографию.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PhotoCameraComponent : Component
{
    /// <summary>Задержка между снимками, чтобы не спамили.</summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1.5);
}
