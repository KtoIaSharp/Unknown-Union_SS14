using System;
using System.Linq;
using Content.Shared._UU.Faction;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._UU.FactionSystem;

public sealed class FactionCitizenSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    private const string WriteTag = "Write";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionEntryDocumentComponent, GetVerbsEvent<Verb>>(OnEntryVerb);
        SubscribeLocalEvent<FactionEntryDocumentComponent, ExaminedEvent>(OnEntryExamine);
    }

    private void OnEntryExamine(Entity<FactionEntryDocumentComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Signatures.Count == 0)
            return;

        foreach (var sig in ent.Comp.Signatures)
        {
            var key = string.IsNullOrEmpty(sig.SignKey) ? "faction-signed-male" : sig.SignKey;
            args.PushMarkup(Loc.GetString(key, ("name", (object)sig.Name), ("date", (object)sig.Date)));
        }
    }

    private void OnEntryVerb(Entity<FactionEntryDocumentComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using == null || !_tag.HasTag(args.Using.Value, WriteTag))
            return;

        if (TryComp<FactionComponent>(args.User, out var faction) && !string.IsNullOrEmpty(faction.Faction))
            return;

        var factionId = ent.Comp.FactionId;
        var factionName = ent.Comp.FactionName;
        if (factionId == null || string.IsNullOrWhiteSpace(factionName))
            return;

        var user = args.User;
        var userName = Name(user);
        var gameDate = new DateTime(2700, 1, 1).AddDays((int)(_timing.CurTime.TotalMinutes / 20));
        var date = gameDate.ToString("dd.MM.yyyy");
        var sex = TryComp<HumanoidProfileComponent>(user, out var profile) ? profile.Sex : Sex.Unsexed;
        var signKey = sex switch
        {
            Sex.Male => "faction-signed-male",
            Sex.Female => "faction-signed-female",
            _ => "faction-signed-neuter",
        };

        var signCategory = new VerbCategory("verb-sign-root", null);

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("verb-sign-entry-document", ("name", (object)factionName)),
            Category = signCategory,
            Priority = 10,
            Act = () =>
            {
                var userFaction = EnsureComp<FactionComponent>(user);
                userFaction.Faction = factionName;
                userFaction.FactionId = factionId;
                userFaction.Title = Loc.GetString("faction-title-member");
                userFaction.IsLeader = false;
                Dirty(user, userFaction);

                if (ent.Comp.Signatures.Any(s => s.Name == userName))
                    return;

                var sprite = SignatureHelper.GetOrAssignSignatureSprite(user, _random, EntityManager);

                ent.Comp.Signatures.Add(new SignatureInfo
                {
                    Name = userName,
                    Date = date,
                    SignKey = signKey,
                    Sprite = sprite,
                });
                Dirty(ent, ent.Comp);

                // Also stamp for UI display
                if (TryComp<PaperComponent>(ent, out var paperComp))
                {
                    _paper.TryStamp((ent.Owner, paperComp), new StampDisplayInfo
                    {
                        StampedName = userName,
                        StampedColor = Color.FromHex("#222222"),
                        SpriteState = sprite,
                    }, "paper_stamp-generic");
                }

                _popup.PopupEntity(Loc.GetString("faction-joined", ("name", (object)factionName)), user, user);
            },
        });
    }
}
