using System.Numerics;
using Content.Shared._UU.Identity;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client._UU.Identity;

/// <summary>
///     Портрет по слепку фотографии. Рисуется прямоугольниками прямо в UI:
///     никаких текстур и ImageSharp, поэтому не зависит от песочницы контента.
///     Это паспортное фото — только лицо, волосы, борода и базовая одежда.
/// </summary>
public sealed class PhotoPortrait : Control
{
    private static readonly Color Background = Color.FromHex("#1B2029");
    private static readonly Color Empty = Color.FromHex("#232A35");
    private static readonly Color Torso = Color.FromHex("#3B4A5E");
    private static readonly Color Outline = Color.FromHex("#31383F");

    private PhotoSnapshot? _snapshot;

    public PhotoSnapshot? Snapshot
    {
        get => _snapshot;
        set
        {
            _snapshot = value;
            InvalidateMeasure();
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (PixelSize.X <= 0 || PixelSize.Y <= 0)
            return;

        var bounds = UIBox2.FromDimensions(Vector2.Zero, PixelSize);
        handle.DrawRect(bounds, _snapshot == null ? Empty : Background);

        if (_snapshot == null)
        {
            DrawOutline(handle, bounds);
            return;
        }

        var skin = _snapshot.SkinColor;

        // Плечи и шея.
        handle.DrawRect(Frac(bounds, 0.14f, 0.66f, 0.86f, 1.0f), Torso);
        handle.DrawRect(Frac(bounds, 0.42f, 0.56f, 0.58f, 0.72f), skin);

        // Волосы рисуются под головой, чтобы причёска читалась шапкой волос.
        if (!string.IsNullOrEmpty(_snapshot.HairStyle))
            handle.DrawRect(Frac(bounds, 0.28f, 0.12f, 0.72f, 0.42f), _snapshot.HairColor);

        // Голова.
        handle.DrawRect(Frac(bounds, 0.34f, 0.16f, 0.66f, 0.62f), skin);

        // Уши и нос — мелочи, без которых лицо читается как пустой прямоугольник.
        handle.DrawRect(Frac(bounds, 0.30f, 0.32f, 0.34f, 0.42f), skin);
        handle.DrawRect(Frac(bounds, 0.66f, 0.32f, 0.70f, 0.42f), skin);
        handle.DrawRect(Frac(bounds, 0.48f, 0.36f, 0.52f, 0.44f), Darken(skin));

        // Глаза.
        handle.DrawRect(Frac(bounds, 0.40f, 0.30f, 0.45f, 0.36f), Outline);
        handle.DrawRect(Frac(bounds, 0.55f, 0.30f, 0.60f, 0.36f), Outline);

        // Борода и усы.
        if (!string.IsNullOrEmpty(_snapshot.FacialHairStyle))
            handle.DrawRect(Frac(bounds, 0.36f, 0.46f, 0.64f, 0.62f), _snapshot.FacialHairColor);

        // Воротник одежды.
        handle.DrawRect(Frac(bounds, 0.30f, 0.72f, 0.70f, 0.80f), Torso);

        DrawOutline(handle, bounds);
    }

    private static UIBox2 Frac(UIBox2 bounds, float x0, float y0, float x1, float y1)
    {
        return new UIBox2(
            bounds.Left + bounds.Width * x0,
            bounds.Top + bounds.Height * y0,
            bounds.Left + bounds.Width * x1,
            bounds.Top + bounds.Height * y1);
    }

    private static Color Darken(Color color)
    {
        return new Color(color.R * 0.75f, color.G * 0.75f, color.B * 0.75f, color.A);
    }

    private static void DrawOutline(DrawingHandleScreen handle, UIBox2 bounds)
    {
        const float thickness = 1f;
        handle.DrawRect(new UIBox2(bounds.Left, bounds.Top, bounds.Right, bounds.Top + thickness), Outline);
        handle.DrawRect(new UIBox2(bounds.Left, bounds.Bottom - thickness, bounds.Right, bounds.Bottom), Outline);
        handle.DrawRect(new UIBox2(bounds.Left, bounds.Top, bounds.Left + thickness, bounds.Bottom), Outline);
        handle.DrawRect(new UIBox2(bounds.Right - thickness, bounds.Top, bounds.Right, bounds.Bottom), Outline);
    }
}
