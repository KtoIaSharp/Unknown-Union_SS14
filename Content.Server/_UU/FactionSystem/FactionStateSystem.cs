using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared._UU.Faction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._UU.FactionSystem;

public sealed class FactionStateSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    private const string WriteTag = "Write";

    // Правка: новое название государства — свободный ввод из QuickDialog, ограничиваем длину.
    private const int MaxFactionNameLength = 48;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionGovernComponent, GetVerbsEvent<Verb>>(OnGovernVerb);
    }

    private void OnGovernVerb(Entity<FactionGovernComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using == null || !_tag.HasTag(args.Using.Value, WriteTag))
            return;

        var user = args.User;

        if (!TryComp<FactionComponent>(user, out var userFaction) ||
            string.IsNullOrEmpty(userFaction.Faction) || !userFaction.IsLeader)
            return;

        var factionId = userFaction.FactionId;
        var factionName = userFaction.Faction;
        if (factionId == null || string.IsNullOrWhiteSpace(factionName))
            return;

        // Правка: управляющий документ должен принадлежать тому же государству, что и докладывающий.
        // Иначе любой лидер получал бы вербы переименования/роспуска по чужому документу.
        if (ent.Comp.FactionId != factionId)
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        // Rename
        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("verb-rename-faction"),
            Priority = 10,
            Act = () =>
            {
                _quickDialog.OpenDialog(actor.PlayerSession,
                    Loc.GetString("faction-rename-dialog-title"),
                    Loc.GetString("faction-rename-dialog-prompt"),
                    (string newName) =>
                    {
                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            _popup.PopupEntity(Loc.GetString("faction-create-invalid-name"), user, user);
                            return;
                        }

                        RenameFaction(factionId.Value, factionName, newName);
                    });
            },
        });

        // Disband
        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("verb-disband-faction"),
            Priority = 5,
            Act = () =>
            {
                _quickDialog.OpenDialog(actor.PlayerSession,
                    Loc.GetString("faction-disband-dialog-title"),
                    Loc.GetString("faction-disband-dialog-prompt"),
                    (string confirm) =>
                    {
                        if (confirm.ToLower() != Loc.GetString("faction-disband-confirm-keyword").ToLower())
                        {
                            _popup.PopupEntity(Loc.GetString("faction-disband-cancelled"), user, user);
                            return;
                        }

                        DisbandFaction(factionId.Value, factionName);
                    });
            },
        });
    }

    /// <summary>
    /// Обрезает пользовательский ввод до разумной длины. Возвращает null, если текста нет.
    /// </summary>
    private static string? ClampFactionName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        return trimmed.Length > MaxFactionNameLength ? trimmed[..MaxFactionNameLength] : trimmed;
    }

    private void RenameFaction(int factionId, string oldName, string newName)
    {
        // Правка: имя из QuickDialog режем по длине, иначе оно уходит в анонс и бумагу без ограничений.
        newName = ClampFactionName(newName) ?? newName;

        var query = EntityQuery<FactionComponent>();
        foreach (var comp in query)
        {
            if (comp.FactionId == factionId)
            {
                comp.Faction = newName;
                Dirty(comp.Owner, comp);
            }
        }

        foreach (var comp in EntityQuery<FactionDocumentComponent>())
            if (comp.FactionId == factionId) { comp.FactionName = newName; Dirty(comp.Owner, comp); }
        foreach (var comp in EntityQuery<FactionEntryDocumentComponent>())
            if (comp.FactionId == factionId) { comp.FactionName = newName; Dirty(comp.Owner, comp); }
        foreach (var comp in EntityQuery<FactionLeaveDocumentComponent>())
            if (comp.FactionId == factionId) { comp.FactionName = newName; Dirty(comp.Owner, comp); }
        foreach (var comp in EntityQuery<FactionGovernComponent>())
            if (comp.FactionId == factionId) { comp.FactionName = newName; Dirty(comp.Owner, comp); }

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("faction-renamed-announcement", ("old", (object)FormattedMessage.EscapeText(oldName)), ("new", (object)FormattedMessage.EscapeText(newName))),
            sender: Loc.GetString("faction-announcement-sender"),
            playSound: false,
            colorOverride: Color.FromHex("#FFD700"));
    }

    private void DisbandFaction(int factionId, string factionName)
    {
        var query = EntityQuery<FactionComponent>();
        foreach (var comp in query)
        {
            if (comp.FactionId == factionId)
            {
                comp.Faction = null;
                comp.FactionId = null;
                comp.Title = null;
                comp.IsLeader = false;
                Dirty(comp.Owner, comp);
            }
        }

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("faction-disbanded-announcement", ("name", (object)FormattedMessage.EscapeText(factionName))),
            sender: Loc.GetString("faction-announcement-sender"),
            playSound: false,
            colorOverride: Color.FromHex("#FFD700"));
    }
}
