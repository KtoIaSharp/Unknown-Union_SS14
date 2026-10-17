"""Генератор иконок слотов экипировки UU.

Иконки слотов лежат не в RSI, а отдельными PNG 32x32 в Resources/Textures/Interface/<тема>/Slots/.
Скрипт берёт штатную иконку слота id.png, стирает её глиф и рисует поверх свой,
используя горизонтальные полосы фона темы. Поэтому новые слоты выглядят родными.

Запуск:
    python Tools/gen_slot_icons.py            # все темы
    python Tools/gen_slot_icons.py Default    # одна тема
    python Tools/gen_slot_icons.py Default --preview
"""

from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SLOTS_DIR = ROOT / "Resources" / "Textures" / "Interface"
SIZE = 32

# Внутренняя область иконки, где рисуется глиф (без рамки).
CORE_X = (7, 24)
CORE_Y = (5, 26)

TRANSPARENT = (0, 0, 0, 0)


def theme_names() -> list[str]:
    return sorted(p.name for p in SLOTS_DIR.iterdir() if (p / "Slots").is_dir())


def _saturation(color: tuple[int, int, int, int]) -> int:
    r, g, b = color[:3]
    return max(r, g, b) - min(r, g, b)


def clean_background(theme: str) -> tuple[Image.Image, Counter]:
    """Возвращает фон слота без глифа и палитру глифа этой темы."""
    path = SLOTS_DIR / theme / "Slots" / "id.png"
    if not path.exists():
        raise FileNotFoundError(path)

    source = Image.open(path).convert("RGBA")
    if source.size != (SIZE, SIZE):
        raise ValueError(f"{path}: ожидался {SIZE}x{SIZE}, получен {source.size}")
    px = source.load()

    core = Counter(
        px[x, y]
        for y in range(CORE_Y[0], CORE_Y[1] + 1)
        for x in range(CORE_X[0], CORE_X[1] + 1)
        if px[x, y][3] > 0
    )
    top = core.most_common(2)
    # Полосы: если в ядре есть второй заметный цвет — у темы есть фон и рамка.
    striped = len(top) > 1 and top[1][1] >= 40

    bg = Image.new("RGBA", (SIZE, SIZE), TRANSPARENT)
    bpx = bg.load()

    if striped:
        # перерисовываем ядро горизонтальными полосами, повторяя рисунок темы
        for y in range(CORE_Y[0] - 1, CORE_Y[1] + 2):
            row = Counter(
                px[x, y]
                for x in range(CORE_X[0], CORE_X[1] + 1)
                if px[x, y][3] > 0
            )
            if not row:
                continue
            color = row.most_common(1)[0][0]
            for x in range(CORE_X[0] - 1, CORE_X[1] + 2):
                bpx[x, y] = color

        # рамку и углы берём из оригинала как есть
        for y in range(SIZE):
            for x in range(SIZE):
                if not (CORE_X[0] - 1 <= x <= CORE_X[1] + 1 and CORE_Y[0] - 1 <= y <= CORE_Y[1] + 1):
                    bpx[x, y] = px[x, y]

        glyph = Counter(
            (pixel, count)
            for pixel, count in core.items()
            if pixel != bpx[min(max(CORE_X[0], 0), SIZE - 1), min(max(CORE_Y[0], 0), SIZE - 1)]
            and (pixel[0], pixel[1], pixel[2], pixel[3]) not in set(bg.get_flattened_data())
        )
    else:
        # Глиф-иконка без рамки: фон прозрачный, все цвета темы — цвета глифа.
        glyph = Counter(dict(core))

    return bg, glyph


def shade(color: tuple[int, int, int], factor: float) -> tuple[int, int, int, int]:
    return (
        max(0, min(255, int(color[0] * factor))),
        max(0, min(255, int(color[1] * factor))),
        max(0, min(255, int(color[2] * factor))),
        255,
    )


def pick_primary(glyph: Counter) -> tuple[int, int, int]:
    """Основной цвет глифа — самый насыщенный (чтобы не взять тёмный контур)."""
    candidates = [(color, count) for color, count in glyph.items() if count >= 3 and color[3] > 0]
    if not candidates:
        return (81, 113, 134)
    candidates.sort(key=lambda item: (_saturation(item[0]), item[1]), reverse=True)
    return candidates[0][0][:3]


class Canvas:
    """Мини-холст для пиксель-арта."""

    def __init__(self, image: Image.Image, primary: tuple[int, int, int]) -> None:
        self.image = image
        self.px = image.load()
        self.primary = (*primary, 255)
        self.dark = shade(primary, 0.62)
        self.light = shade(primary, 1.32)

    def dot(self, x: int, y: int, color) -> None:
        if 0 <= x < SIZE and 0 <= y < SIZE:
            self.px[x, y] = color

    def rect(self, x0: int, y0: int, x1: int, y1: int, color) -> None:
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, color)

    def frame(self, x0: int, y0: int, x1: int, y1: int, color) -> None:
        for x in range(x0, x1 + 1):
            self.dot(x, y0, color)
            self.dot(x, y1, color)
        for y in range(y0, y1 + 1):
            self.dot(x0, y, color)
            self.dot(x1, y, color)

    def vline(self, x: int, y0: int, y1: int, color) -> None:
        for y in range(y0, y1 + 1):
            self.dot(x, y, color)

    def hline(self, x0: int, x1: int, y: int, color) -> None:
        for x in range(x0, x1 + 1):
            self.dot(x, y, color)


def draw_passport(c: Canvas) -> None:
    """Паспорт: вертикальная книжка с гербом и строками."""
    c.rect(8, 5, 23, 26, c.primary)
    c.frame(8, 5, 23, 26, c.dark)
    # корешок
    c.rect(8, 5, 10, 26, c.dark)
    # кромка страниц справа
    c.vline(23, 6, 25, c.light)
    # поперечная полоса на обложке
    c.hline(11, 22, 7, c.dark)
    # герб: кольцо
    c.rect(14, 10, 18, 14, c.light)
    c.rect(15, 11, 17, 13, c.dark)
    # строки
    c.hline(13, 20, 17, c.light)
    c.hline(13, 19, 19, c.light)
    c.hline(13, 20, 21, c.light)
    # нижняя плашка
    c.rect(12, 23, 19, 25, c.light)


def draw_position(c: Canvas) -> None:
    """Должностной документ: лист с печатью."""
    c.rect(6, 6, 25, 25, c.primary)
    c.frame(6, 6, 25, 25, c.dark)
    # строки текста
    c.hline(9, 19, 9, c.dark)
    c.hline(9, 16, 11, c.dark)
    c.hline(9, 19, 13, c.dark)
    c.hline(9, 14, 15, c.dark)
    # печать
    c.rect(16, 17, 22, 23, c.dark)
    c.rect(17, 18, 21, 22, c.primary)
    c.rect(18, 19, 20, 21, c.light)


PAINTERS = {"passport": draw_passport, "position": draw_position}


def build(theme: str, slot: str, preview: bool) -> Path:
    background, glyph = clean_background(theme)
    canvas = Canvas(background.copy(), pick_primary(glyph))
    PAINTERS[slot](canvas)

    target = SLOTS_DIR / theme / "Slots" / f"{slot}.png"
    canvas.image.save(target)

    if preview:
        out = ROOT / "sprites_preview"
        out.mkdir(exist_ok=True)
        canvas.image.resize((SIZE * 6, SIZE * 6), Image.NEAREST).save(out / f"slot_{theme}_{slot}_6x.png")

    return target


def main() -> int:
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    preview = "--preview" in sys.argv
    themes = args or theme_names()

    for theme in themes:
        for slot in PAINTERS:
            path = build(theme, slot, preview)
            print(f"written {path.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
