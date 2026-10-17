using Robust.Shared.GameObjects;

namespace Content.Server._UU.Radio;

/// <summary>
/// Код доступа к сигнальной вышке.
/// Компонент живёт только на сервере: клиент никогда не получает сам код,
/// только факт «вышка заперта». Так знание кода остаётся настоящим секретом,
/// который передают из рук в руки, а не строкой в сетевом состоянии.
/// </summary>
[RegisterComponent]
public sealed partial class SignalTowerAccessComponent : Component
{
    [DataField]
    public string? Code;
}
