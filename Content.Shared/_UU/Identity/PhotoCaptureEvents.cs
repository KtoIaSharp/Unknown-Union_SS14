using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._UU.Identity;

/// <summary>
///     Сервер просит клиента фотографа снять спрайт цели. Клиент не может сделать это
///     за сервер: пиксели есть только у него, и только пока цель в PVS.
/// </summary>
[Serializable, NetSerializable]
public sealed class PhotoCaptureRequestEvent : EntityEventArgs
{
    /// <summary>Фотография, в которую нужно положить снимок.</summary>
    public NetEntity Photo;

    /// <summary>Кого фотографируют.</summary>
    public NetEntity Target;
}

/// <summary>
///     Ответ клиента: PNG спрайта в base64 (или null, если снять не удалось).
/// </summary>
[Serializable, NetSerializable]
public sealed class PhotoCaptureResponseEvent : EntityEventArgs
{
    public NetEntity Photo;
    public string? ImageBase64;
}
