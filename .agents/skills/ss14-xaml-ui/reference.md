# XAML reference for this fork

Verified against this working copy. Nothing here is imported from another SS14 version.

## Why there is no designer, and where the loader lives

- XAML is compiled by XamlIL at build time:
  `Content.Client/Content.Client.csproj:27` → `RobustToolbox/MSBuild/XamlIL.targets`.
- That target sets `<EmbeddedResource Include="**/*.xaml"/>`,
  `<AdditionalFiles Include="**/*.xaml"/>` and excludes `*.xaml` from `DefaultItemExcludes`,
  so **adding a `.xaml` file needs no project edit**.
- Errors appear as ordinary MSBuild errors (`CompileRobustXamlTask`), so
  `dotnet build Content.Client/Content.Client.csproj` is the fastest feedback loop.
- `RobustXamlLoader.Load(this)` is what applies the file to the instance; it must run before
  any generated `Name` field is touched.

## Real examples to copy from

| File | Shows |
|---|---|
| `Content.Client/Access/UI/IdCardConsoleWindow.xaml` | minimal `DefaultWindow` root, `GridContainer`, `MinSize`, almost no StyleClasses |
| `Content.Client/Corvax/SecApartment/SecApartmentWindow.xaml` + `.xaml.cs` | full custom window: own `ui:` root, `gfx:StyleBoxFlat` panels, `TabContainer`, dynamic child controls from code, `Dispose` restoring the global stylesheet |
| `Content.Client/Corvax/SecApartment/CrewMemberEntry.xaml` | small reusable entry control added via `AddChild` |
| `Content.Client/Access/UI/AccessLevelControl.xaml` | list-style control generated per item |
| `Content.Client/Administration/UI/BanList/Bans/BanListLine.xaml` | row control with `Access="Public"` fields used by the parent |

## Controls and attributes actually used

Containers: `BoxContainer` (`Orientation`, `SeparationOverride`), `GridContainer` (`Columns`,
`HSeparationOverride`), `ScrollContainer` (`HScrollEnabled`, `VScrollEnabled`),
`PanelContainer`, `TabContainer`, `Control` (spacer), `StripeBack`
(`Content.Client.UserInterface.Controls`, needs `xmlns:controls`).

Leaves: `Label`, `RichTextLabel`, `LineEdit` (`PlaceHolder`, `Text`, `IsSecret`), `Button`
(`Text`, `Disabled`), `TextureButton`, `TextureRect` (`TexturePath`, `Stretch`),
`CheckBox`, `OptionButton`, `ItemList`, `Slider`, `ProgressBar`, `TextureRect`.

Layout attributes: `HorizontalExpand`, `VerticalExpand`, `HorizontalAlignment`,
`VerticalAlignment`, `MinSize`, `MinWidth`, `MinHeight`, `SetHeight`, `SetWidth`,
`SetSize`, `Margin`, `RectClipContent`, `MouseFilter`, `Modulate`, `ModulateSelfOverride`,
`StyleClasses`, `Access`, `Name`, `Visible`.

`PanelContainer.PanelOverride` with a `gfx:StyleBoxFlat`
(`xmlns:gfx="clr-namespace:Robust.Client.Graphics;assembly=Robust.Client"`) is how custom
flat panels/borders are drawn:

```xml
<PanelContainer StyleClasses="AngleRect">
    <PanelContainer.PanelOverride>
        <gfx:StyleBoxFlat BackgroundColor="#330000" BorderColor="#ff4444" BorderThickness="2" />
    </PanelContainer.PanelOverride>
    <!-- content -->
</PanelContainer>
```

## Window root options

- Inheriting directly from `DefaultWindow` → root tag is `<DefaultWindow ...>`
  (see `IdCardConsoleWindow`). Children go to `ContentsContainer` automatically
  (`DefaultWindow.xaml.cs:63` sets `Contents = ContentsContainer`).
- Owning a dedicated control class → declare your own prefix:
  `<ui:MyWindow xmlns:ui="clr-namespace:Content.Client.Path.To.It" ...>`.
- Window chrome parts exposed by `DefaultWindow`: `WindowHeader`, `ContentsContainer`,
  `CloseButton`, and style class constants `windowTitle`, `windowPanel`, `windowHeader`,
  `windowCloseButton`.

## Full verified StyleClass list (85 names)

Extracted from `StyleClass.cs` constant values plus every `.Class("...")` literal under
`Content.Client/Stylesheets/**` (including `Sheetlets/`). Regenerate with the command in
SKILL.md; do not copy this list into another SS14 version.

```
BackgroundDark, BackgroundOpenLeft, BackgroundOpenRight, BackgroundPanel,
BackgroundPanelDark, BackgroundPanelOpenLeft, BackgroundPanelOpenRight,
BorderedWindowPanel, bubbleContent, ButtonAccept, ButtonBig, ButtonColorGreen,
ButtonColorRed, ButtonSmall, ButtonSquare, Caution, ConsoleHeading, ConsoleSubHeading,
ConsoleText, CrossButtonRed, Danger, DefaultBorderBottom, DefaultBorderTop, Disabled,
FancyWindowTitle, font-large, font-small, Good, HelpButton, HighDivider, highlight,
InsetBackground, italic, ItemStatus, ItemStatusNotHeld, LabelHeading, LabelHeadingBigger,
LabelKeyText, LabelSubText, LabelWeak, LowDivider, monospace, negative, NTLogoDark,
OpenBoth, OpenLeft, OpenRight, PanelBackgroundBaseDark, PanelBackgroundLight, PanelDark,
PanelDropTarget, PanelLight, PaperDefaultBorder, PaperLineEdit, PaperWrittenText,
PdaBackground, PdaBackgroundRect, PdaBorderRect, PdaContentBackground,
PdaContentFooterText, PdaWindowFooterText, positive, RadialMenuBackButton,
RadialMenuButton, RadialMenuCloseButton, RefreshButton, SpeciesInfoDefault,
SpeciesInfoWarning, status-bad, status-critical, StatusFieldTitle, status-good,
status-okay, status-warning, TooltipDesc, TooltipPanel, TooltipTitle,
transparentBackgroundItemList, transparentItemList, WindowContentsContainer,
WindowFooterText, windowHeaderAlert, WindowHeadingBackground,
WindowHeadingBackgroundLight, windowTitleAlert
```

Windows that need a bespoke look (like `SecApartmentWindow`) build their own
`StyleRule[]` with `StylesheetHelpers` and merge them with the current
`UserInterfaceManager.Stylesheet` — copy that pattern instead of inventing class names.

## Localization keys used by the converted radio windows

`radio-transmitter-title`, `radio-jammer-title`, `radio-enter-frequency`,
`radio-enter-name`, `radio-frequency-placeholder`, `radio-name-placeholder`,
`radio-set-frequency`, `radio-no-frequency`, `radio-current-frequency`,
`radio-jammer-no-target`, `radio-jammer-target` —
all already present in `Resources/Locale/{ru-RU,en-US}/_UU/radio.ftl`.

## Converting a code-built window: checklist

1. Create `Foo.xaml` with the layout; put `{Loc}` in every text/placeholder/title.
2. Move behaviour into `Foo.xaml.cs`, mark it `[GenerateTypedNameReferences] partial`,
   call `RobustXamlLoader.Load(this);` first.
3. `Name="..."` each control the code touches; keep the same public API
   (`events`, `UpdateState(...)`) so the `BoundUserInterface` needs no change.
4. **Delete the old `Foo.cs`** — otherwise the class is declared twice.
5. Keep dynamic per-item controls (`new CrewMemberEntry(...)`) in code; they are added with
   `AddChild` to a container declared in XAML.
6. Build `Content.Client` and open the window in game.
