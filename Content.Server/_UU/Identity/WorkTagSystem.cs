using System.Collections.Generic;
using Content.Shared._UU.Faction;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.Server._UU.Identity;

/// <summary>
///     Реестр рабочих тегов. Теги принадлежат государству и живут, пока оно существует:
///     после роспуска государства его теги перестают предлагаться.
/// </summary>
public sealed class WorkTagSystem : EntitySystem
{
    private readonly Dictionary<int, List<string>> _tags = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _tags.Clear();
    }

    /// <summary>Существует ли ещё государство с таким id (скан по игрокам с <see cref="FactionComponent"/>).</summary>
    public bool FactionExists(int factionId)
    {
        var query = EntityQueryEnumerator<FactionComponent>();
        while (query.MoveNext(out _, out var faction))
        {
            if (faction.FactionId == factionId)
                return true;
        }

        return false;
    }

    /// <summary>Теги государства; если государство распущено — пусто, а теги забываются.</summary>
    public IReadOnlyList<string> GetTags(int factionId)
    {
        if (!FactionExists(factionId))
        {
            _tags.Remove(factionId);
            return new List<string>();
        }

        return _tags.TryGetValue(factionId, out var list) ? list : new List<string>();
    }

    /// <summary>Добавить тег государству. Возвращает false, если такой уже есть.</summary>
    public bool TryAddTag(int factionId, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        var trimmed = Clamp(tag);

        if (!_tags.TryGetValue(factionId, out var list))
        {
            list = new List<string>();
            _tags[factionId] = list;
        }

        if (list.Contains(trimmed))
            return false;

        list.Add(trimmed);
        return true;
    }

    public void RemoveTag(int factionId, string tag)
    {
        if (_tags.TryGetValue(factionId, out var list))
            list.Remove(tag);
    }

    public static string Clamp(string tag)
    {
        var trimmed = tag.Trim();
        return trimmed.Length > 32 ? trimmed[..32] : trimmed;
    }
}
