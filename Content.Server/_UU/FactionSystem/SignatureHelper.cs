using Content.Shared._UU.Faction;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._UU.FactionSystem;

public static class SignatureHelper
{
    public static readonly string[] SignatureSprites =
        { "sig01", "sig02", "sig03", "sig04", "sig05", "sig06", "sig07", "sig08", "sig09", "sig10" };

    /// <summary>
    /// Возвращает спрайт подписи игрока.
    /// Если ещё не выбран — выбирает случайный и фиксирует до конца раунда.
    /// </summary>
    public static string GetOrAssignSignatureSprite(EntityUid player, IRobustRandom random, IEntityManager entMan)
    {
        if (entMan.TryGetComponent<FactionComponent>(player, out var faction) &&
            !string.IsNullOrEmpty(faction.SignatureSprite))
        {
            return faction.SignatureSprite;
        }

        var sprite = random.Pick(SignatureSprites);

        if (faction != null)
        {
            faction.SignatureSprite = sprite;
            entMan.Dirty(player, faction);
        }
        else
        {
            var temp = entMan.EnsureComponent<FactionComponent>(player);
            temp.SignatureSprite = sprite;
            entMan.Dirty(player, temp);
        }

        return sprite;
    }
}
