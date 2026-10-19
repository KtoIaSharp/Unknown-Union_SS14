using System;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared._UU.Faction;
using Content.Shared._UU.Office;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Nutrition.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._UU.FactionSystem;

public sealed class FactionSystem : EntitySystem
{
    [Dependency] private readonly Content.Server._UU.CellSystem.CellSystem _cellSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string WriteTag = "Write";

    // Правка: название государства — свободный пользовательский ввод из QuickDialog, который
    // уходит в глобальные анонсы, метаданные бумаги и сетевое состояние. Ограничиваем длину.
    private const int MaxFactionNameLength = 48;

    private int _nextFactionId = 1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<FactionComponent, GetVerbsEvent<Verb>>(OnFactionVerb);
        SubscribeLocalEvent<FactionSignableComponent, ExaminedEvent>(OnSignableExamine);
        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<Verb>>(OnPaperVerb);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        // Правка: счётчик ID государств — статическое поле системы, живёт между раундами,
        // поэтому его обязательно сбрасываем при рестарте (состояние на компонентах не трогаем).
        _nextFactionId = 1;
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

    private void OnSignableExamine(Entity<FactionSignableComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        foreach (var sig in ent.Comp.Signatures)
        {
            var key = string.IsNullOrEmpty(sig.SignKey) ? "faction-signed-male" : sig.SignKey;
            // Правка: имя подписанта вставляется в markup-сообщение — экранируем.
            args.PushMarkup(Loc.GetString(key, ("name", (object)FormattedMessage.EscapeText(sig.Name)), ("date", (object)sig.Date)));
        }
    }

    private void OnExamine(Entity<FactionComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (string.IsNullOrEmpty(ent.Comp.Faction))
            args.PushMarkup(Loc.GetString("faction-none"));
        else
        {
            args.PushMarkup(Loc.GetString("faction-examine", ("faction", (object)FormattedMessage.EscapeText(ent.Comp.Faction!))));
            if (!string.IsNullOrEmpty(ent.Comp.Title) &&
                TryComp<FactionComponent>(args.Examiner, out var examinerFaction) &&
                examinerFaction.FactionId == ent.Comp.FactionId)
            {
                args.PushMarkup(Loc.GetString("faction-title-examine", ("title", (object)FormattedMessage.EscapeText(ent.Comp.Title!))));
            }
        }
    }

    private void OnFactionVerb(Entity<FactionComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var leader = args.User;
        var citizen = ent; // the entity being right-clicked

        // Only leader can expel
        if (!TryComp<FactionComponent>(leader, out var leaderFaction) ||
            !leaderFaction.IsLeader || leaderFaction.FactionId == null)
            return;

        // Target must be a citizen of the same state, not the leader
        if (citizen.Comp.FactionId != leaderFaction.FactionId || citizen.Comp.IsLeader)
            return;

        var factionName = leaderFaction.Faction ?? "???";
        var targetName = Name(citizen);

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("verb-expel-citizen", ("name", (object)targetName)),
            Priority = 10,
            Act = () =>
            {
                citizen.Comp.Faction = null;
                citizen.Comp.FactionId = null;
                citizen.Comp.Title = null;
                citizen.Comp.IsLeader = false;
                Dirty(citizen, citizen.Comp);

                _popup.PopupEntity(
                    Loc.GetString("faction-expelled", ("name", (object)FormattedMessage.EscapeText(targetName)), ("faction", (object)FormattedMessage.EscapeText(factionName))),
                    citizen, leader);
                _popup.PopupEntity(
                    Loc.GetString("faction-expelled-you", ("faction", (object)FormattedMessage.EscapeText(factionName))),
                    citizen, citizen);
            },
        });
    }

    private void OnPaperVerb(Entity<PaperComponent> paper, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using == null || !_tag.HasTag(args.Using.Value, WriteTag))
            return;

        var user = args.User;
        var hasFaction = TryComp<FactionComponent>(user, out var userFaction) &&
                         !string.IsNullOrEmpty(userFaction.Faction);
        var faction = userFaction;

        var isSignedDoc = HasComp<FactionDocumentComponent>(paper) ||
                          HasComp<FactionEntryDocumentComponent>(paper) ||
                          HasComp<FactionLeaveDocumentComponent>(paper) ||
                          HasComp<FactionGovernComponent>(paper);

        // Создание государственных бумаг и документов ячеек переехало в печатную машинку:
        // верб на бумаге больше нет, бумага остаётся бумагой до момента печати (см. TypewriterSystem).

        // Категория для группировки подписей
        var signCategory = new VerbCategory("verb-sign-root", null);

        if (!hasFaction || faction == null)
            return;

        // --- Верб "Подписать" с сабвербами ---
        if (string.IsNullOrWhiteSpace(paper.Comp.Content))
            return;

        var userName = Name(user);
        var time = _timing.CurTime;
        var gameDate = new DateTime(2700, 1, 1).AddDays((int)(time.TotalMinutes / 20));
        var date = gameDate.ToString("dd.MM.yyyy");
        var sex = TryComp<HumanoidProfileComponent>(user, out var profile) ? profile.Sex : Sex.Unsexed;
        var signKey = sex switch
        {
            Sex.Male => "faction-signed-male",
            Sex.Female => "faction-signed-female",
            _ => "faction-signed-neuter",
        };

        // Сабверб 1: Личная подпись (на любой бумаге с текстом, не на гос. документах)
        if (!isSignedDoc)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("verb-personal-sign"),
                Category = signCategory,
                Priority = 2,
                Act = () =>
                {
                    var signable = EnsureComp<FactionSignableComponent>(paper);
                    if (signable.Signatures.Any(s => s.Name == userName))
                        return;

                    // Правка: список подписей — сетевое поле, его рост без лимита раздувает сетевой стейт.
                    if (signable.Signatures.Count >= FactionSignatureLimits.MaxSignatures)
                    {
                        _popup.PopupEntity(Loc.GetString("faction-sign-limit-reached"), user, user);
                        return;
                    }

                    var sprite = SignatureHelper.GetOrAssignSignatureSprite(user, _random, EntityManager);

                    signable.Signatures.Add(new SignatureInfo
                    {
                        Name = userName,
                        Date = date,
                        SignKey = signKey,
                        Sprite = sprite,
                    });
                    Dirty(paper, signable);

                    _paper.TryStamp(paper, new StampDisplayInfo
                    {
                        StampedName = userName,
                        StampedColor = Color.FromHex("#222222"),
                        SpriteState = sprite,
                    }, "paper_stamp-generic");

                    _popup.PopupEntity(Loc.GetString("faction-personal-signed"), user, user);
                },
            });
        }

        // Сабверб 2: Штамп государства (только лидер, не на гос. документах)
        if (hasFaction && faction!.IsLeader && !isSignedDoc)
        {
            var factionName = faction.Faction!;

            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("verb-state-sign", ("name", (object)factionName)),
                Category = signCategory,
                Priority = 1,
                Act = () =>
                {
                    var stampInfo = new StampDisplayInfo
                    {
                        StampedName = factionName,
                        StampedColor = Color.FromHex("#FFD700"),
                    };
                    _paper.TryStamp(paper, stampInfo, "paper_stamp-generic");

                    _popup.PopupEntity(Loc.GetString("faction-state-signed", ("name", (object)FormattedMessage.EscapeText(factionName))), user, user);
                },
            });
        }
    }

    /// <summary>
    ///     Печать государственной формы на листе из печатной машинки.
    ///     Возвращает false и локализационный ключ ошибки, если состояние игрока не подходит.
    /// </summary>
    public bool TryPrintForm(FormKind kind, Entity<PaperComponent> paper, EntityUid user, out string? error)
    {
        error = null;

        TryComp<FactionComponent>(user, out var factionData);
        var factionName = factionData?.Faction;
        var hasFaction = !string.IsNullOrEmpty(factionName);

        switch (kind)
        {
            case FormKind.FactionFounding:
            {
                if (hasFaction)
                {
                    error = "typewriter-faction-already";
                    return false;
                }

                if (!TryComp<ActorComponent>(user, out var actor))
                {
                    error = "typewriter-no-actor";
                    return false;
                }

                _quickDialog.OpenDialog(actor.PlayerSession,
                    Loc.GetString("faction-create-dialog-title"),
                    Loc.GetString("faction-create-dialog-prompt"),
                    (string newName) =>
                    {
                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            _popup.PopupEntity(Loc.GetString("faction-create-invalid-name"), user, user);
                            return;
                        }

                        CreateFaction(user, newName, paper);
                    });

                return true;
            }

            case FormKind.FactionCell:
            {
                if (!_cellSystem.CanCreateCell(user))
                {
                    error = "typewriter-cell-unavailable";
                    return false;
                }

                if (!TryComp<ActorComponent>(user, out var cellActor))
                {
                    error = "typewriter-no-actor";
                    return false;
                }

                _quickDialog.OpenDialog(cellActor.PlayerSession,
                    Loc.GetString("cell-create-dialog-title"),
                    Loc.GetString("cell-create-dialog-prompt"),
                    (string cellName) =>
                    {
                        if (string.IsNullOrWhiteSpace(cellName))
                        {
                            _popup.PopupEntity(Loc.GetString("cell-create-invalid-name"), user, user);
                            return;
                        }

                        CreateCellDocument(user, cellName, paper);
                    });

                return true;
            }

            case FormKind.FactionEntry:
            case FormKind.FactionGovern:
            {
                if (!hasFaction || factionData?.FactionId is not { } fid)
                {
                    error = "typewriter-faction-missing";
                    return false;
                }

                if (!factionData.IsLeader)
                {
                    error = "typewriter-not-leader";
                    return false;
                }

                if (kind == FormKind.FactionEntry)
                    CreateEntryDocument(user, fid, factionName!, paper);
                else
                    CreateGovernDocument(user, fid, factionName!, paper);

                return true;
            }

            case FormKind.FactionLeave:
            {
                if (!hasFaction || factionData?.FactionId is not { } fid)
                {
                    error = "typewriter-faction-missing";
                    return false;
                }

                if (factionData.IsLeader)
                {
                    error = "typewriter-leader-cannot-leave";
                    return false;
                }

                LeaveFaction(user, fid, paper);
                return true;
            }

            default:
                error = "typewriter-unknown-form";
                return false;
        }
    }

    private void LeaveFaction(EntityUid player, int factionId, Entity<PaperComponent> paper)
    {
        if (!TryComp<FactionComponent>(player, out var faction) || faction.FactionId != factionId)
            return;

        var name = faction.Faction ?? "???";
        var comment = paper.Comp.Content;

        faction.Faction = null;
        faction.FactionId = null;
        faction.Title = null;
        faction.IsLeader = false;
        Dirty(player, faction);

        var doc = EnsureComp<FactionLeaveDocumentComponent>(paper);
        doc.FactionName = name;
        doc.FactionId = factionId;
        doc.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;

        WriteDocumentText(paper, "faction-doc-leave", ("name", (object)name), comment);

        _meta.SetEntityName(paper, Loc.GetString("faction-leave-document-name", ("name", (object)FormattedMessage.EscapeText(name))));
        _popup.PopupEntity(Loc.GetString("faction-left", ("name", (object)FormattedMessage.EscapeText(name))), player, player);
    }

    private void CreateEntryDocument(EntityUid player, int factionId, string factionName, Entity<PaperComponent> paper)
    {
        var comment = paper.Comp.Content;

        var doc = EnsureComp<FactionEntryDocumentComponent>(paper);
        doc.FactionName = factionName;
        doc.FactionId = factionId;
        doc.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;

        WriteDocumentText(paper, "faction-doc-entry", ("name", (object)factionName), comment);

        EnsureComp<BlockWritingComponent>(paper);
        paper.Comp.EditingDisabled = true;
        DirtyEntity(paper);

        _meta.SetEntityName(paper, Loc.GetString("faction-entry-document-name", ("name", (object)FormattedMessage.EscapeText(factionName))));
        _meta.SetEntityDescription(paper, Loc.GetString("faction-entry-document-description", ("name", (object)FormattedMessage.EscapeText(factionName))));

        var stampInfo = new StampDisplayInfo { StampedName = FormattedMessage.EscapeText(factionName), StampedColor = Color.FromHex("#FFD700") };
        _paper.TryStamp(paper, stampInfo, "paper_stamp-generic");

        _popup.PopupEntity(Loc.GetString("faction-entry-document-created", ("name", (object)FormattedMessage.EscapeText(factionName))), player, player);
    }

    private void CreateGovernDocument(EntityUid player, int factionId, string factionName, Entity<PaperComponent> paper)
    {
        var comment = paper.Comp.Content;

        var doc = EnsureComp<FactionGovernComponent>(paper);
        doc.FactionName = factionName;
        doc.FactionId = factionId;
        doc.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;

        WriteDocumentText(paper, "faction-doc-govern", ("name", (object)factionName), comment);

        EnsureComp<BlockWritingComponent>(paper);
        paper.Comp.EditingDisabled = true;
        DirtyEntity(paper);

        _meta.SetEntityName(paper, Loc.GetString("faction-govern-document-name", ("name", (object)FormattedMessage.EscapeText(factionName))));
        _meta.SetEntityDescription(paper, Loc.GetString("faction-govern-document-description", ("name", (object)FormattedMessage.EscapeText(factionName))));

        var stampInfo = new StampDisplayInfo { StampedName = FormattedMessage.EscapeText(factionName), StampedColor = Color.FromHex("#FFD700") };
        _paper.TryStamp(paper, stampInfo, "paper_stamp-generic");

        _popup.PopupEntity(Loc.GetString("faction-govern-document-created", ("name", (object)FormattedMessage.EscapeText(factionName))), player, player);
    }

    private void CreateCellDocument(EntityUid player, string cellName, Entity<PaperComponent> paper)
    {
        var comment = paper.Comp.Content;

        // Правка: ячейка может не создаться (лимит ячеек) — тогда и документ выдавать нельзя,
        // иначе на руках остаётся бумага о несуществующей ячейке.
        if (!_cellSystem.CreateCell(player, cellName, out cellName))
            return;

        // Правка: название из QuickDialog не должно уходить в бумагу без ограничения длины.
        cellName = ClampFactionName(cellName) ?? cellName;

        WriteDocumentText(paper, "faction-doc-cell", ("name", (object)cellName), comment);

        EnsureComp<BlockWritingComponent>(paper);
        paper.Comp.EditingDisabled = true;
        DirtyEntity(paper);

        _meta.SetEntityName(paper, Loc.GetString("cell-create-document-name", ("name", (object)FormattedMessage.EscapeText(cellName))));
    }

    private void CreateFaction(EntityUid player, string factionName, Entity<PaperComponent> paper)
    {
        // Правка: режем пользовательский ввод из QuickDialog до разумной длины (см. MaxFactionNameLength).
        factionName = ClampFactionName(factionName) ?? factionName;

        var factionId = _nextFactionId++;
        var comment = paper.Comp.Content;

        var factionComp = EnsureComp<FactionComponent>(player);
        factionComp.Faction = factionName;
        factionComp.FactionId = factionId;
        factionComp.Title = Loc.GetString("faction-title-leader");
        factionComp.IsLeader = true;

        var doc = EnsureComp<FactionDocumentComponent>(paper);
        doc.FactionName = factionName;
        doc.FactionId = factionId;
        doc.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;

        WriteDocumentText(paper, "faction-doc-founding", ("name", (object)factionName), comment);

        EnsureComp<BlockWritingComponent>(paper);
        RemCompDeferred<EdibleComponent>(paper);

        _meta.SetEntityName(paper, Loc.GetString("faction-document-name", ("name", (object)FormattedMessage.EscapeText(factionName))));
        _meta.SetEntityDescription(paper, Loc.GetString("faction-document-description", ("name", (object)FormattedMessage.EscapeText(factionName))));
        DirtyEntity(paper);

        var stampInfo = new StampDisplayInfo { StampedName = FormattedMessage.EscapeText(factionName), StampedColor = Color.FromHex("#FFD700") };
        _paper.TryStamp(paper, stampInfo, "paper_stamp-generic");

        _popup.PopupEntity(Loc.GetString("faction-created", ("name", (object)FormattedMessage.EscapeText(factionName))), player, player);

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("faction-created-announcement", ("name", (object)FormattedMessage.EscapeText(factionName)), ("player", (object)Name(player))),
            sender: Loc.GetString("faction-announcement-sender"),
            playSound: false,
            colorOverride: Color.FromHex("#FFD700"));
    }

    private void WriteDocumentText(Entity<PaperComponent> paper, string localeKey, (string, object) nameParam, string? comment)
    {
        // Правка: и текст шаблона, и комментарий автора попадают в markup бумаги — экранируем
        // пользовательские значения, чтобы их нельзя было разметить внутри документа.
        var safeName = (nameParam.Item1, (object)FormattedMessage.EscapeText(nameParam.Item2?.ToString() ?? string.Empty));
        var text = Loc.GetString(localeKey, safeName);
        if (!string.IsNullOrWhiteSpace(comment))
            text += $"\n\n--- {Loc.GetString("faction-doc-comment-label")} ---\n{FormattedMessage.EscapeText(comment)}";

        paper.Comp.Content = text;
        DirtyEntity(paper);
    }
}
