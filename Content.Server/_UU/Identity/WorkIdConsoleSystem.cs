using System.Collections.Generic;
using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._UU.Employment;
using Content.Shared._UU.Faction;
using Content.Shared._UU.Identity;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server._UU.Identity;

/// <summary>
///     Консоль работодателя: заполняет удостоверение, переносит на него фотографию,
///     создаёт рабочие теги государства и хранит списки доступа.
/// </summary>
public sealed class WorkIdConsoleSystem : EntitySystem
{
    private const int MaxFieldLength = 48;

    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly WorkTagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WorkIdConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WorkIdConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<WorkIdConsoleComponent, ContainerModifiedMessage>(OnSlotChanged);
        SubscribeLocalEvent<WorkIdConsoleComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<WorkIdConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<WorkIdConsoleComponent, WorkIdConsoleApplyMessage>(OnApply);
        SubscribeLocalEvent<WorkIdConsoleComponent, WorkIdConsoleAccessMessage>(OnAccess);
    }

    private void OnInit(Entity<WorkIdConsoleComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, WorkIdConsoleComponent.CardSlotId, ent.Comp.CardSlot);
        _itemSlots.AddItemSlot(ent.Owner, WorkIdConsoleComponent.PhotoSlotId, ent.Comp.PhotoSlot);
    }

    private void OnRemove(Entity<WorkIdConsoleComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.CardSlot);
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.PhotoSlot);
    }

    private void OnSlotChanged(Entity<WorkIdConsoleComponent> ent, ref ContainerModifiedMessage args)
    {
        if (args.Container.ID == ent.Comp.CardSlot.ID || args.Container.ID == ent.Comp.PhotoSlot.ID)
            UpdateBui(ent, ent.Comp.CardSlot.Item.HasValue);
    }

    private void OnVerb(Entity<WorkIdConsoleComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("work-console-verb-open"),
            Priority = 10,
            Act = () => Open(ent, user),
        });

        if (ent.Comp.FactionId == null && TryComp<FactionComponent>(user, out var faction) && faction.IsLeader
            && faction.FactionId is { } ownId && !string.IsNullOrWhiteSpace(faction.Faction))
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("work-console-verb-bind"),
                Priority = 8,
                Act = () => BindFaction(ent, ownId, faction.Faction!, user),
            });
        }
    }

    private void BindFaction(Entity<WorkIdConsoleComponent> ent, int factionId, string factionName, EntityUid user)
    {
        ent.Comp.FactionId = factionId;
        ent.Comp.FactionName = factionName;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("work-console-bound", ("name", (object)FormattedMessage.EscapeText(factionName))), user, user);
        UpdateBui(ent, true);
    }

    private void Open(Entity<WorkIdConsoleComponent> ent, EntityUid user)
    {
        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("work-console-no-power"), user, user);
            return;
        }

        if (!CanUse(ent, user))
        {
            _popup.PopupEntity(Loc.GetString("work-console-no-access"), user, user);
            return;
        }

        _ui.OpenUi(ent.Owner, WorkIdConsoleUiKey.Console, user);
    }

    private void OnUiOpened(Entity<WorkIdConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is WorkIdConsoleUiKey.Console)
            UpdateBui(ent, true);
        else if (args.UiKey is WorkIdConsoleUiKey.Access)
            UpdateBui(ent, true);
    }

    private void OnApply(Entity<WorkIdConsoleComponent> ent, ref WorkIdConsoleApplyMessage args)
    {
        var user = args.Actor;

        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("work-console-no-power"), user, user);
            return;
        }

        if (!CanUse(ent, user))
        {
            _popup.PopupEntity(Loc.GetString("work-console-no-access"), user, user);
            return;
        }

        if (ent.Comp.CardSlot.Item is not { } cardUid || !TryComp<WorkIdCardComponent>(cardUid, out var card))
        {
            _popup.PopupEntity(Loc.GetString("work-console-no-card"), user, user);
            return;
        }

        card.Holder = Clamp(args.Holder);
        card.Position = Clamp(args.Position);
        card.Employer = Clamp(args.Employer);
        card.Workplace = Clamp(args.Workplace);
        card.Date ??= DateTime.UtcNow.ToString("dd.MM.yyyy");

        if (!string.IsNullOrWhiteSpace(args.Tag))
        {
            var tag = WorkTagSystem.Clamp(args.Tag);

            if (ent.Comp.FactionId is { } factionId)
            {
                // Создавать теги может любой, кому разрешено пользоваться консолью.
                _tags.TryAddTag(factionId, tag);
            }

            card.Tag = tag;
        }

        if (args.ApplyPhoto
            && ent.Comp.PhotoSlot.Item is { } photoUid
            && TryComp<WorkPhotoComponent>(photoUid, out var photo)
            && photo.Snapshot != null)
        {
            card.Photo = photo.Snapshot;
            QueueDel(photoUid);
            _popup.PopupEntity(Loc.GetString("work-console-photo-applied"), user, user);
        }

        card.Issued = true;
        Dirty(cardUid, card);
        _popup.PopupEntity(Loc.GetString("work-console-applied"), user, user);
        UpdateBui(ent, true);
    }

    private void OnAccess(Entity<WorkIdConsoleComponent> ent, ref WorkIdConsoleAccessMessage args)
    {
        var user = args.Actor;

        if (!CanEdit(ent, user))
        {
            _popup.PopupEntity(Loc.GetString("work-console-no-access"), user, user);
            return;
        }

        ent.Comp.UseTags = args.UseTags.Select(WorkTagSystem.Clamp).Distinct().ToList();
        ent.Comp.EditTags = args.EditTags.Select(WorkTagSystem.Clamp).Distinct().ToList();
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("work-console-access-saved"), user, user);
        UpdateBui(ent, true);
    }

    /// <summary>Пустой список «кто может пользоваться» пускает всех, иначе — по тегу карты.</summary>
    public bool CanUse(Entity<WorkIdConsoleComponent> ent, EntityUid user)
    {
        if (ent.Comp.UseTags.Count == 0)
            return true;

        return UserHasAnyTag(user, ent.Comp.UseTags);
    }

    /// <summary>Если список «кто может редактировать» пуст — права те же, что на пользование.</summary>
    public bool CanEdit(Entity<WorkIdConsoleComponent> ent, EntityUid user)
    {
        if (ent.Comp.EditTags.Count == 0)
            return CanUse(ent, user);

        return UserHasAnyTag(user, ent.Comp.EditTags);
    }

    private bool UserHasAnyTag(EntityUid user, List<string> tags)
    {
        foreach (var slot in new[] { InventorySystem.PositionSlotName, InventorySystem.PassportSlotName })
        {
            if (!_inventory.TryGetSlotEntity(user, slot, out var item) || item is null)
                continue;

            if (TryComp<WorkIdCardComponent>(item.Value, out var card)
                && !string.IsNullOrWhiteSpace(card.Tag)
                && tags.Contains(card.Tag!))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateBui(Entity<WorkIdConsoleComponent> ent, bool updateCard)
    {
        var comp = ent.Comp;
        WorkIdCardComponent? card = null;

        if (comp.CardSlot.Item is { } cardUid)
            TryComp(cardUid, out card);

        PhotoSnapshot? photo = null;
        if (comp.PhotoSlot.Item is { } photoUid && TryComp<WorkPhotoComponent>(photoUid, out var photoComp))
            photo = photoComp.Snapshot;

        var tags = comp.FactionId is { } factionId
            ? _tags.GetTags(factionId).ToList()
            : new List<string>();

        // Открывающий окно: считаем права по его карте.
        var actors = _ui.GetActors(ent.Owner, WorkIdConsoleUiKey.Console);
        var actor = actors.FirstOrDefault();

        var canEdit = actor != EntityUid.Invalid && CanEdit(ent, actor);
        var state = new WorkIdConsoleBuiState(
            card?.Holder,
            card?.Position,
            card?.Employer,
            card?.Date,
            card?.EmployerSignatureLabel,
            card?.Workplace,
            card?.Tag,
            card?.Photo ?? photo,
            comp.CardSlot.Item.HasValue,
            comp.PhotoSlot.Item.HasValue,
            canEdit,
            comp.FactionName,
            tags,
            comp.UseTags.ToList(),
            comp.EditTags.ToList());

        _ui.SetUiState(ent.Owner, WorkIdConsoleUiKey.Console, state);

        var accessActors = _ui.GetActors(ent.Owner, WorkIdConsoleUiKey.Access);
        if (accessActors.Any())
            _ui.SetUiState(ent.Owner, WorkIdConsoleUiKey.Access, state);

        _ = updateCard;
    }

    private static string? Clamp(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        return trimmed.Length > MaxFieldLength ? trimmed[..MaxFieldLength] : trimmed;
    }
}
