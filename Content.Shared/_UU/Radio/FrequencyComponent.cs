using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Radio;

[RegisterComponent, NetworkedComponent]
public sealed partial class TransmitterComponent : Component
{
    [DataField]
    public string? Frequency;

    [DataField]
    public string? FrequencyName;

    /// <summary>
    /// Can only be configured once. Reset on destruction/deconstruction.
    /// </summary>
    [DataField]
    public bool Configured;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class RadioComponent : Component
{
    [DataField]
    public List<string> Frequencies = new();

    /// <summary>
    /// Whether this radio transmits messages. Toggled with Z (UseInHand).
    /// Off = receive only.
    /// </summary>
    [DataField]
    public bool TransmitEnabled = true;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class EarpieceComponent : Component
{
    [DataField]
    public List<string> Frequencies = new();
}

/// <summary>
/// Stores a frequency, can copy from transmitter and paste to devices.
/// Cannot send/receive messages itself.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FrequencyConfiguratorComponent : Component
{
    [DataField]
    public string? StoredFrequency;

    [DataField]
    public string? StoredFrequencyName;
}

[Serializable, NetSerializable]
public sealed class FrequencyBuiState : BoundUserInterfaceState
{
    public readonly string? Frequency;
    public readonly string? FrequencyName;
    public readonly List<string> AvailableFrequencies;
    public readonly bool Configured;

    public FrequencyBuiState(string? frequency, List<string> available, string? name = null,
        bool configured = false)
    {
        Frequency = frequency;
        AvailableFrequencies = available;
        FrequencyName = name;
        Configured = configured;
    }
}

[Serializable, NetSerializable]
public sealed class FrequencySetMessage : BoundUserInterfaceMessage
{
    public readonly string Frequency;
    public readonly string? Name;

    public FrequencySetMessage(string frequency, string? name = null)
    {
        Frequency = frequency;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class RadioSendMessage : BoundUserInterfaceMessage
{
    public readonly string Message;
    public RadioSendMessage(string message) => Message = message;
}

[Serializable, NetSerializable]
public enum FrequencyUiKey
{
    Transmitter,
    Radio,
    Jammer,
}
