using System.Collections.Generic;
using System.Linq;
using Content.Server._UU.FactionSystem;
using Content.Server.Power.EntitySystems;
using Content.Shared._UU.Employment;
using Content.Shared._UU.Faction;
using Content.Server._UU.Employment;
using Content.Shared._UU.Identity;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._UU.Identity;

/// <summary>Реестр одного государства: должности, теги и списки доступа его консолей.</summary>
public sealed class WorkStateRegistry
{
    public string Name = string.Empty;
    public readonly List<WorkRegistryEntry> Entries = new();
    public readonly List<string> Tags = new();
    public readonly List<string> UseTags = new();
    public readonly List<string> EditTags = new();
}

/// <summary>
///     Консоль управления должностями и сканер должности.
///     Консоль и сканер закрепляются за государством; реестр у каждого государства свой.
///     Сканер вносит в реестр имя, должность, тег и того, кто назначил.
///     Увольнение не трогает предмет-карту: оно отзывает связку «человек + тег» в реестре.
/// </summary>
public sealed class WorkRegistrySystem : EntitySystem
{
    public const int MaxTagLength = 32;

    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly FactionBoundSystem _factionBound = default!;
    [Dependency] private readonly WorkIdCardSystem _cards = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<int, WorkStateRegistry> _registries = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WorkRegistryConsoleComponent, ActivateInWorldEvent>(OnConsoleActivate);
        SubscribeLocalEvent<WorkRegistryConsoleComponent, BoundUIOpenedEvent>(OnConsoleOpened);
        SubscribeLocalEvent<WorkRegistryConsoleComponent, WorkRegistryRevokeMessage>(OnRevoke);
        SubscribeLocalEvent<WorkRegistryConsoleComponent, WorkRegistryAccessMessage>(OnAccess);

        SubscribeLocalEvent<PositionScannerComponent, UseInHandEvent>(OnScannerUseInHand);
        SubscribeLocalEvent<PositionScannerComponent, AfterInteractEvent>(OnScannerInteract);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _registries.Clear();
    }

    #region Реестр

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

    public WorkStateRegistry GetRegistry(int factionId, string? factionName)
    {
        if (!_registries.TryGetValue(factionId, out var registry))
        {
            registry = new WorkStateRegistry();
            _registries[factionId] = registry;
        }

        if (!string.IsNullOrWhiteSpace(factionName))
            registry.Name = factionName;

        return registry;
    }

    /// <summary>Добавить или обновить запись по связке «имя + должность».</summary>
    public void UpsertEntry(int factionId, string? factionName, WorkRegistryEntry entry)
    {
        var registry = GetRegistry(factionId, factionName);

        var existing = registry.Entries.FirstOrDefault(e =>
            e.Name == entry.Name && e.Position == entry.Position);

        if (existing != null)
        {
            existing.Tag = entry.Tag;
            existing.Employer = entry.Employer;
            existing.Date = entry.Date;
            existing.Revoked = false;
            AddTag(registry, entry.Tag);
            return;
        }

        registry.Entries.Add(entry);
        AddTag(registry, entry.Tag);
    }

    private static void AddTag(WorkStateRegistry registry, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        var clamped = Clamp(tag);

        if (!registry.Tags.Contains(clamped))
            registry.Tags.Add(clamped);
    }

    /// <summary>
    ///     Отозван ли тег у человека. Проверяется по всем государствам:
    ///     карта, выданная в одном государстве, не должна срабатывать по правилам другого.
    /// </summary>
    public bool IsRevoked(string? holder, string? tag)
    {
        if (string.IsNullOrWhiteSpace(holder) || string.IsNullOrWhiteSpace(tag))
            return false;

        foreach (var registry in _registries.Values)
        {
            foreach (var entry in registry.Entries)
            {
                if (entry.Revoked && entry.Name == holder && entry.Tag == tag)
                    return true;
            }
        }

        return false;
    }

    public static string Clamp(string tag)
    {
        var trimmed = tag.Trim();
        return trimmed.Length > MaxTagLength ? trimmed[..MaxTagLength] : trimmed;
    }

    #endregion

    #region Консоль

    private void OnConsoleActivate(Entity<WorkRegistryConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        Open(ent, args.User);
        args.Handled = true;
    }

    private void Open(Entity<WorkRegistryConsoleComponent> ent, EntityUid user)
    {
        if (!_factionBound.IsBound(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("work-registry-unbound"), user, user);
            return;
        }

        if (!_power.IsPowered(ent.Owner))
            _popup.PopupEntity(Loc.GetString("work-registry-no-power"), user, user);

        if (!CanUse(ent.Comp, user))
        {
            _popup.PopupEntity(Loc.GetString("work-registry-no-access"), user, user);
            return;
        }

        _ui.OpenUi(ent.Owner, WorkRegistryUiKey.Registry, user);
    }

    private void OnConsoleOpened(Entity<WorkRegistryConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateBui(ent);
    }

    private void OnRevoke(Entity<WorkRegistryConsoleComponent> ent, ref WorkRegistryRevokeMessage args)
    {
        var user = args.Actor;

        if (!CanEdit(ent.Comp, user))
        {
            _popup.PopupEntity(Loc.GetString("work-registry-no-access"), user, user);
            return;
        }

        if (!_factionBound.TryGetFaction(ent.Owner, out var factionId, out var factionName))
            return;

        var registry = GetRegistry(factionId, factionName);

        var name = args.Name;
        var position = args.Position;
        var tag = args.Tag;
        var entry = registry.Entries.FirstOrDefault(e =>
            e.Name == name && e.Position == position && (e.Tag ?? string.Empty) == tag);

        if (entry == null)
        {
            _popup.PopupEntity(Loc.GetString("work-registry-entry-missing"), user, user);
            return;
        }

        entry.Revoked = args.Revoke;

        SyncCards(entry, args.Revoke, factionName);

        _popup.PopupEntity(
            Loc.GetString(args.Revoke ? "work-registry-fired" : "work-registry-restored",
                ("name", (object)FormattedMessage.EscapeText(entry.Name))),
            user,
            user);

        UpdateBui(ent);
    }

    /// <summary>Перенести отзыв на сами карты: реестр живёт до первого роспуска государства, карта — нет.</summary>
    private void SyncCards(WorkRegistryEntry entry, bool revoke, string? factionName)
    {
        var date = GameDate();

        var cards = EntityQueryEnumerator<WorkIdCardComponent>();
        while (cards.MoveNext(out var cardUid, out var card))
        {
            if (card.Holder != entry.Name)
                continue;

            if ((card.Tag ?? string.Empty) != (entry.Tag ?? string.Empty))
                continue;

            card.RegistryRevoked = revoke;
            card.RegistryRevokedBy = revoke ? factionName : null;
            card.RegistryRevokedAt = revoke ? date : null;
            Dirty(cardUid, card);

            _cards.Refresh(cardUid);
        }
    }

    private string GameDate()
    {
        return new DateTime(2700, 1, 1).AddDays((int) (_timing.CurTime.TotalMinutes / 20)).ToString("dd.MM.yyyy");
    }

    private void OnAccess(Entity<WorkRegistryConsoleComponent> ent, ref WorkRegistryAccessMessage args)
    {
        var user = args.Actor;

        if (!CanEdit(ent.Comp, user))
        {
            _popup.PopupEntity(Loc.GetString("work-registry-no-access"), user, user);
            return;
        }

        ent.Comp.UseTags = args.UseTags.Select(Clamp).Where(t => t.Length > 0).Distinct().ToList();
        ent.Comp.EditTags = args.EditTags.Select(Clamp).Where(t => t.Length > 0).Distinct().ToList();
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("work-registry-access-saved"), user, user);
        UpdateBui(ent);
    }

    /// <summary>Пустой список «кто может пользоваться» пускает всех, иначе — по активному тегу карты.</summary>
    public bool CanUse(WorkRegistryConsoleComponent comp, EntityUid user)
    {
        if (comp.UseTags.Count == 0)
            return true;

        return UserHasAnyTag(user, comp.UseTags);
    }

    /// <summary>Пустой список «кто может редактировать» = права пользования.</summary>
    public bool CanEdit(WorkRegistryConsoleComponent comp, EntityUid user)
    {
        if (comp.EditTags.Count == 0)
            return CanUse(comp, user);

        return UserHasAnyTag(user, comp.EditTags);
    }

    private bool UserHasAnyTag(EntityUid user, List<string> tags)
    {
        foreach (var slot in new[] { InventorySystem.PositionSlotName, InventorySystem.PassportSlotName })
        {
            if (!_inventory.TryGetSlotEntity(user, slot, out var item) || item is null)
                continue;

            if (!TryComp<WorkIdCardComponent>(item.Value, out var card) || string.IsNullOrWhiteSpace(card.Tag))
                continue;

            if (!tags.Contains(card.Tag!))
                continue;

            // Уволенный тег не даёт доступа даже с картой в руках.
            if (card.RegistryRevoked || IsRevoked(card.Holder, card.Tag))
                continue;

            return true;
        }

        return false;
    }

    private void UpdateBui(Entity<WorkRegistryConsoleComponent> ent)
    {
        var comp = ent.Comp;
        var registry = _factionBound.TryGetFaction(ent.Owner, out var factionId, out var factionName)
            ? GetRegistry(factionId, factionName)
            : new WorkStateRegistry();

        var actors = _ui.GetActors(ent.Owner, WorkRegistryUiKey.Registry).ToList();
        var actor = actors.Count > 0 ? actors[0] : EntityUid.Invalid;

        var state = new WorkRegistryBuiState(
            factionName,
            registry.Entries.Select(e => new WorkRegistryEntry
            {
                Name = e.Name,
                Position = e.Position,
                Tag = e.Tag,
                Employer = e.Employer,
                Date = e.Date,
                Revoked = e.Revoked,
            }).ToList(),
            registry.Tags.ToList(),
            comp.UseTags.ToList(),
            comp.EditTags.ToList(),
            actor != EntityUid.Invalid && CanUse(comp, actor),
            actor != EntityUid.Invalid && CanEdit(comp, actor),
            _power.IsPowered(ent.Owner));

        _ui.SetUiState(ent.Owner, WorkRegistryUiKey.Registry, state);

        if (_ui.GetActors(ent.Owner, WorkRegistryUiKey.Access).Any())
            _ui.SetUiState(ent.Owner, WorkRegistryUiKey.Access, state);
    }

    #endregion

    #region Сканер

    /// <summary>Сканер в руке: обычное использование сканирует карту во второй руке.</summary>
    private void OnScannerUseInHand(Entity<PositionScannerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;

        foreach (var item in _hands.EnumerateHeld((args.User, hands)))
        {
            if (item == ent.Owner)
                continue;

            if (HasComp<WorkIdCardComponent>(item) && TryScan(ent, item, args.User))
            {
                args.Handled = true;
                return;
            }
        }

        _popup.PopupEntity(Loc.GetString("position-scanner-no-card"), args.User, args.User);
    }

    /// <summary>Сканер по лежащей карте: клик сканером по удостоверению.</summary>
    private void OnScannerInteract(Entity<PositionScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        if (!HasComp<WorkIdCardComponent>(target))
            return;

        if (TryScan(ent, target, args.User))
            args.Handled = true;
    }

    private bool TryScan(Entity<PositionScannerComponent> scanner, EntityUid cardUid, EntityUid user)
    {
        if (!_factionBound.TryGetFaction(scanner.Owner, out var factionId, out var factionName))
        {
            _popup.PopupEntity(Loc.GetString("position-scanner-unbound"), user, user);
            return false;
        }

        if (TryComp<WorkIdCardComponent>(cardUid, out var stamped))
        {
            if (stamped.RegistryRevoked)
            {
                _popup.PopupEntity(Loc.GetString("position-scanner-revoked"), user, user);
                return false;
            }

            if (!stamped.RegistryStamped)
            {
                _popup.PopupEntity(Loc.GetString("position-scanner-no-stamp"), user, user);
                return false;
            }

            if (stamped.RegistryStampFactionId is { } stampFaction && stampFaction != factionId)
            {
                _popup.PopupEntity(Loc.GetString("position-scanner-wrong-state"), user, user);
                return false;
            }
        }

        if (!TryComp<WorkIdCardComponent>(cardUid, out var card)
            || string.IsNullOrWhiteSpace(card.Holder)
            || string.IsNullOrWhiteSpace(card.Position))
        {
            _popup.PopupEntity(Loc.GetString("position-scanner-empty"), user, user);
            return false;
        }

        // Тег по умолчанию — сама должность: отдельного поля для тега на карте нет,
        // а реестр должен уметь отличать должности друг от друга.
        var tag = Clamp(string.IsNullOrWhiteSpace(card.Tag) ? card.Position! : card.Tag!);
        card.Tag = tag;
        Dirty(cardUid, card);

        UpsertEntry(factionId, factionName, new WorkRegistryEntry
        {
            Name = card.Holder!,
            Position = card.Position!,
            Tag = tag,
            Employer = card.Employer,
            Date = card.Date,
        });

        _popup.PopupEntity(
            Loc.GetString("position-scanner-scanned",
                ("name", (object)FormattedMessage.EscapeText(card.Holder!)),
                ("position", (object)FormattedMessage.EscapeText(card.Position!))),
            user,
            user);

        // Обновляем открытые консоли этого государства, чтобы запись появилась сразу.
        var consoles = EntityQueryEnumerator<WorkRegistryConsoleComponent>();
        while (consoles.MoveNext(out var consoleUid, out var console))
        {
            if (_factionBound.TryGetFaction(consoleUid, out var consoleFaction, out _)
                && consoleFaction == factionId)
            {
                UpdateBui((consoleUid, console));
            }
        }

        return true;
    }

    #endregion
}
