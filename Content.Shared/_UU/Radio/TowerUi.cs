using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Radio;

/// <summary>
/// Состояние вышки для спрайта: просто стоит, ретранслирует, заглушена или обесточена.
/// </summary>
[Serializable, NetSerializable]
public enum SignalTowerState : byte
{
    Idle,
    Active,
    Jammed,
    Off,
}

/// <summary>
/// Ключи визуального состояния вышки для GenericVisualizer.
/// </summary>
[Serializable, NetSerializable]
public enum SignalTowerVisuals : byte
{
    State,
}

/// <summary>
/// Что клиент знает о вышке: частоту и то, заперта ли она кодом.
/// Сам код на клиент не уходит — иначе «узкий круг посвящённых» теряет смысл.
/// </summary>
[Serializable, NetSerializable]
public sealed class TowerBuiState : BoundUserInterfaceState
{
    public readonly string? Frequency;
    public readonly bool HasCode;

    public TowerBuiState(string? frequency, bool hasCode)
    {
        Frequency = frequency;
        HasCode = hasCode;
    }
}

/// <summary>
/// Настройка вышки: предъявить код (или задать его впервые) и выставить частоту.
/// </summary>
[Serializable, NetSerializable]
public sealed class TowerConfigureMessage : BoundUserInterfaceMessage
{
    public readonly string Code;
    public readonly string Frequency;

    public TowerConfigureMessage(string code, string frequency)
    {
        Code = code;
        Frequency = frequency;
    }
}
