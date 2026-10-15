using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Включает batch-режим отрисовки XAML-окон клиента в PNG.
    /// Аналог autogen.entity_screenshot, но рисует не сущности, а интерфейс.
    /// </summary>
    public static readonly CVarDef<bool> UiPreviewEnabled =
        CVarDef.Create("uu.ui_preview.enabled", false, CVar.CLIENTONLY);

    /// <summary>
    /// Список типов окон через запятую: полное имя типа или короткое имя класса Control.
    /// Пример: Content.Client._UU.Radio.TransmitterWindow,Content.Client._UU.Radio.JammerWindow
    /// </summary>
    public static readonly CVarDef<string> UiPreviewWindows =
        CVarDef.Create("uu.ui_preview.windows", string.Empty, CVar.CLIENTONLY);

    /// <summary>
    /// Каталог вывода внутри UserData.
    /// </summary>
    public static readonly CVarDef<string> UiPreviewOutputPath =
        CVarDef.Create("uu.ui_preview.output_path", "/UiPreview", CVar.CLIENTONLY);

    /// <summary>
    /// Сколько кадров прогреть до снимка: раскладка, шрифты, спрайты, стили.
    /// </summary>
    public static readonly CVarDef<int> UiPreviewWarmupFrames =
        CVarDef.Create("uu.ui_preview.warmup_frames", 5, CVar.CLIENTONLY);
}
