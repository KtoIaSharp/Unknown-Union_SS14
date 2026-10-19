using System.Linq;
using Content.Server._UU.Employment;
using Content.Shared._UU.Office;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._UU.Office;

/// <summary>
///     Печатная машинка: на вставленном листе бумаги печатает выбранную форму.
///     Сама бумага предметом не меняется — смысл документа появляется из компонента,
///     который вешает обработчик формы (<see cref="FactionSystem"/> или <see cref="EmploymentContractSystem"/>).
/// </summary>
public sealed class TypewriterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly Content.Server._UU.FactionSystem.FactionSystem _faction = default!;
    [Dependency] private readonly EmploymentContractSystem _employment = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TypewriterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TypewriterComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<TypewriterComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<TypewriterComponent, TypewriterPrintMessage>(OnPrint);
        SubscribeLocalEvent<TypewriterComponent, ContainerModifiedMessage>(OnSlotChanged);
    }

    private void OnInit(Entity<TypewriterComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, TypewriterComponent.PaperSlotId, ent.Comp.PaperSlot);
        UpdateAppearance(ent);
    }

    private void OnRemove(Entity<TypewriterComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.PaperSlot);
    }

    private void OnSlotChanged(Entity<TypewriterComponent> ent, ref ContainerModifiedMessage args)
    {
        if (args.Container.ID != ent.Comp.PaperSlot.ID)
            return;

        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void OnUiOpened(Entity<TypewriterComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnPrint(Entity<TypewriterComponent> ent, ref TypewriterPrintMessage args)
    {
        var user = args.Actor;
        if (user == EntityUid.Invalid)
            return;

        if (!_prototype.TryIndex<FormPrototype>(args.Form, out var form))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, TypewriterComponent.PaperSlotId, out var slot) ||
            slot.Item is not { } paperUid ||
            !TryComp<PaperComponent>(paperUid, out var paper))
        {
            _popup.PopupEntity(Loc.GetString("typewriter-no-paper"), user, user);
            return;
        }

        if (!string.IsNullOrWhiteSpace(paper.Content))
        {
            _popup.PopupEntity(Loc.GetString("typewriter-paper-used"), user, user);
            return;
        }

        string? error;
        var printed = form.Kind == FormKind.EmploymentContract
            ? _employment.TryPrintContract((paperUid, paper), user, out error)
            : _faction.TryPrintForm(form.Kind, (paperUid, paper), user, out error);

        if (!printed)
        {
            if (!string.IsNullOrEmpty(error))
                _popup.PopupEntity(Loc.GetString(error), user, user);

            return;
        }

        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void UpdateAppearance(Entity<TypewriterComponent> ent)
    {
        var hasPaper = ent.Comp.PaperSlot.Item.HasValue;
        _appearance.SetData(ent.Owner, TypewriterVisuals.HasPaper, hasPaper);
    }

    private void UpdateUi(Entity<TypewriterComponent> ent)
    {
        var forms = _prototype
            .EnumeratePrototypes<FormPrototype>()
            .OrderBy(f => f.Order)
            .ThenBy(f => f.ID, StringComparer.Ordinal)
            .Select(f => f.ID)
            .ToList();

        var hasPaper = ent.Comp.PaperSlot.Item.HasValue;
        _ui.SetUiState(ent.Owner, TypewriterUiKey.Key, new TypewriterBuiState(forms, hasPaper));
    }
}
