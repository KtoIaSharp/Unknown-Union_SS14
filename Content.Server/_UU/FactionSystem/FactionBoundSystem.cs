using Content.Server.Popups;
using Content.Shared._UU.Faction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._UU.FactionSystem;

/// <summary>
/// Одна точка привязки сущностей к государству: общий верб и общие проверки.
/// </summary>
public sealed class FactionBoundSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionBoundComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private void OnVerb(Entity<FactionBoundComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.FactionId != null)
            return;

        if (!TryComp<FactionComponent>(args.User, out var faction)
            || faction.FactionId is not { } factionId
            || string.IsNullOrWhiteSpace(faction.Faction))
        {
            return;
        }

        if (ent.Comp.LeaderOnly && !faction.IsLeader)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("faction-bound-verb"),
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_UU/verb_icons.rsi"), "bind_faction"),
            Priority = 8,
            Act = () => Bind(ent, user),
        });
    }

    /// <summary>Закрепить сущность за государством игрока.</summary>
    public bool Bind(Entity<FactionBoundComponent> ent, EntityUid user)
    {
        if (ent.Comp.FactionId != null)
        {
            _popup.PopupEntity(Loc.GetString("faction-bound-already"), user, user);
            return false;
        }

        if (!TryComp<FactionComponent>(user, out var faction)
            || faction.FactionId is not { } factionId
            || string.IsNullOrWhiteSpace(faction.Faction))
        {
            _popup.PopupEntity(Loc.GetString("faction-bound-no-state"), user, user);
            return false;
        }

        if (!FactionExists(factionId))
        {
            _popup.PopupEntity(Loc.GetString("faction-bound-no-state"), user, user);
            return false;
        }

        ent.Comp.FactionId = factionId;
        ent.Comp.FactionName = faction.Faction;
        Dirty(ent.Owner, ent.Comp);

        _popup.PopupEntity(
            Loc.GetString("faction-bound-done", ("name", (object)FormattedMessage.EscapeText(faction.Faction))),
            user,
            user);

        return true;
    }

    /// <summary>Есть ли ещё государство с таким id.</summary>
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

    public bool IsBound(EntityUid uid)
    {
        return TryComp<FactionBoundComponent>(uid, out var bound) && bound.FactionId != null;
    }

    /// <summary>Государство, за которым закреплена сущность.</summary>
    public bool TryGetFaction(EntityUid uid, out int factionId, out string? factionName)
    {
        factionId = 0;
        factionName = null;

        if (!TryComp<FactionBoundComponent>(uid, out var bound) || bound.FactionId is not { } id)
            return false;

        factionId = id;
        factionName = bound.FactionName;
        return true;
    }
}
