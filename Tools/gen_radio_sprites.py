"""Генератор спрайта радиочастотного ретранслятора (_UU/transmitter.rsi).

Рисует 32x32 пиксель-арт в четырёх направлениях (South, North, East, West) так,
как их ожидает Robust RSI: горизонтальная полоса 128x32, по одной 32x32 ячейке
на направление, порядок RsiDirection = South, North, East, West.

Запуск:
    python Tools/gen_transmitter_sprite.py            # записать в RSI
    python Tools/gen_transmitter_sprite.py --preview  # плюс превью 6x
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image

SIZE = 32

# Палитра: тёмный индустриальный металл с фиолетовым акцентом UU.
OUT = (13, 15, 18)        # контур
DARK = (30, 34, 41)       # тёмный корпус
MID = (47, 54, 64)        # основной корпус
LIT = (73, 83, 97)        # светлая грань
HI = (104, 116, 134)      # блик
PANEL = (22, 26, 32)      # утопленная панель
SCREEN = (34, 52, 62)     # экран
GREEN = (99, 212, 113)
AMBER = (255, 179, 71)
RED = (217, 79, 79)
PURPLE = (138, 108, 240)
COPPER = (122, 92, 62)
CABLE = (17, 19, 23)

TRANSPARENT = (0, 0, 0, 0)


class Cell:
    """Одна 32x32 ячейка с примитивами рисования."""

    def __init__(self) -> None:
        self.img = Image.new("RGBA", (SIZE, SIZE), TRANSPARENT)
        self.px = self.img.load()

    def dot(self, x: int, y: int, color) -> None:
        if 0 <= x < SIZE and 0 <= y < SIZE:
            self.px[x, y] = (*color, 255)

    def rect(self, x0: int, y0: int, x1: int, y1: int, color) -> None:
        """Заливка прямоугольника включительно по обеим границам."""
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

    def poly(self, points: list[tuple[int, int]], color) -> None:
        """Ломаная по точкам: утолщённая линия без пропусков."""
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            steps = max(abs(x1 - x0), abs(y1 - y0), 1)
            for step in range(steps + 1):
                x = round(x0 + (x1 - x0) * step / steps)
                y = round(y0 + (y1 - y0) * step / steps)
                self.dot(x, y, color)


def base(c: Cell) -> None:
    """Общее основание: плита с ножками и тонкая стойка-шея."""
    # плита
    c.rect(6, 27, 25, 30, MID)
    c.hline(6, 25, 27, LIT)
    c.hline(6, 25, 30, DARK)
    c.frame(6, 27, 25, 30, OUT)
    # ножки-пятки
    c.rect(7, 31, 10, 31, DARK)
    c.rect(21, 31, 24, 31, DARK)
    # шея стойки
    c.rect(14, 23, 18, 27, DARK)
    c.vline(14, 23, 27, MID)
    c.vline(18, 23, 27, OUT)


def rivets(c: Cell) -> None:
    """Мелкие заклёпки по углам корпуса — читаются как заводская деталь."""
    for x, y in ((9, 7), (22, 7), (9, 24), (22, 24)):
        c.dot(x, y, HI)


def south(c: Cell) -> None:
    """Вид спереди: панель, индикаторы, слот для ключа, провода."""
    base(c)

    # антенна на кронштейне
    c.hline(9, 12, 5, OUT)
    c.vline(11, 1, 4, OUT)
    c.dot(11, 0, AMBER)

    # корпус
    c.rect(8, 5, 23, 26, DARK)
    c.frame(8, 5, 23, 26, OUT)
    c.vline(9, 6, 25, MID)
    c.vline(22, 6, 25, DARK)
    c.hline(8, 23, 5, MID)
    c.hline(9, 22, 6, LIT)
    # фиолетовая полоса брендирования
    c.hline(9, 22, 7, PURPLE)
    rivets(c)

    # панель с внутренней тенью
    c.rect(10, 8, 21, 25, PANEL)
    c.frame(10, 8, 21, 25, OUT)
    c.hline(11, 20, 9, DARK)

    # индикаторы
    c.dot(12, 11, GREEN)
    c.dot(15, 11, AMBER)
    c.dot(18, 11, RED)
    c.hline(11, 20, 12, OUT)

    # экранчик
    c.rect(11, 14, 20, 18, SCREEN)
    c.frame(11, 14, 20, 18, OUT)
    c.hline(12, 19, 15, LIT)
    c.hline(12, 16, 16, DARK)
    c.dot(18, 17, PURPLE)

    # слот под ключ
    c.rect(13, 19, 18, 22, OUT)
    c.rect(14, 20, 17, 21, PURPLE)
    c.hline(15, 16, 20, HI)

    # решётка
    c.hline(11, 20, 24, DARK)

    # провода: отвод справа вниз к плите
    c.poly([(23, 18), (26, 18), (26, 24), (25, 27)], COPPER)
    c.poly([(23, 21), (27, 21), (27, 26), (26, 28)], CABLE)
    c.dot(26, 18, HI)
    c.dot(27, 21, HI)


def north(c: Cell) -> None:
    """Вид сзади: глухой корпус, рёбра, жгут кабелей, разъём питания."""
    base(c)

    c.rect(8, 5, 23, 26, DARK)
    c.frame(8, 5, 23, 26, OUT)
    c.vline(9, 6, 25, MID)
    c.vline(22, 6, 25, DARK)
    c.hline(9, 22, 6, LIT)
    rivets(c)

    # рёбра охлаждения
    for y in range(9, 24, 3):
        c.hline(11, 19, y, MID)
        c.hline(11, 19, y + 1, OUT)

    # жгут кабелей с хомутами
    c.poly([(20, 8), (20, 14), (21, 20), (20, 26)], COPPER)
    c.poly([(21, 10), (21, 16), (22, 22), (21, 27)], CABLE)
    c.hline(19, 21, 12, OUT)
    c.hline(20, 22, 19, OUT)

    # разъём питания
    c.rect(12, 20, 16, 24, OUT)
    c.rect(13, 21, 15, 23, HI)


def tx_side(c: Cell, mirror: bool = False) -> None:
    """Вид сбоку: узкий профиль, кабели позади, индикатор на передней грани."""
    base(c)

    # корпус в профиль
    c.rect(12, 5, 21, 26, DARK)
    c.frame(12, 5, 21, 26, OUT)
    c.vline(13, 6, 25, MID)
    c.vline(20, 6, 25, DARK)
    c.hline(13, 20, 6, LIT)
    # нависающий козырёк
    c.rect(11, 5, 22, 6, MID)
    c.frame(11, 5, 22, 6, OUT)

    # передняя кромка с панелью
    c.vline(14, 8, 24, PANEL)
    c.vline(15, 8, 24, PANEL)
    c.dot(15, 12, GREEN)
    c.dot(15, 16, AMBER)
    c.frame(14, 20, 17, 23, OUT)
    c.rect(15, 21, 16, 22, PURPLE)

    # кабели позади
    c.poly([(22, 11), (23, 16), (22, 22), (21, 27)], COPPER)
    c.poly([(23, 14), (24, 20), (23, 26)], CABLE)
    c.hline(22, 24, 15, OUT)

    if mirror:
        c.img = c.img.transpose(Image.FLIP_LEFT_RIGHT)
        c.px = c.img.load()


def lattice(c: Cell, top_y: int, bot_y: int, tl: int, tr: int, bl: int, br: int, step: int = 5) -> None:
    """Решётчатая мачта: сужающиеся стойки, поперечины и крестовые связи."""
    span = max(bot_y - top_y, 1)

    def edges(y: int) -> tuple[int, int]:
        t = (y - top_y) / span
        return round(tl + (bl - tl) * t), round(tr + (br - tr) * t)

    for y in range(top_y, bot_y + 1):
        left, right = edges(y)
        c.dot(left, y, LIT)
        c.dot(right, y, OUT)
        if (y - top_y) % step == 0:
            c.hline(left, right, y, MID)

    for y0 in range(top_y, bot_y, step):
        y1 = min(y0 + step, bot_y)
        for i in range(y1 - y0 + 1):
            y = y0 + i
            left, right = edges(y)
            ratio = i / max(y1 - y0, 1)
            c.dot(round(left + (right - left) * ratio), y, MID)
            c.dot(round(right - (right - left) * ratio), y, MID)


def mj_south(c: Cell) -> None:
    """Глушитель спереди: предупреждающая полоса, панель, излучатели помех."""
    base(c)

    # корпус
    c.rect(7, 11, 24, 27, DARK)
    c.frame(7, 11, 24, 27, OUT)
    c.vline(8, 12, 26, LIT)
    c.vline(23, 12, 26, OUT)
    c.hline(8, 23, 11, MID)

    # предупреждающая полоса
    for y in range(12, 15):
        for x in range(8, 24):
            c.dot(x, y, AMBER if ((x + y) // 2) % 2 == 0 else OUT)

    # панель
    c.rect(9, 16, 22, 26, PANEL)
    c.frame(9, 16, 22, 26, OUT)

    # индикаторы
    c.rect(11, 18, 12, 19, RED)
    c.dot(11, 18, HI)
    c.dot(20, 18, GREEN)

    # регулятор
    c.rect(17, 21, 21, 25, MID)
    c.frame(17, 21, 21, 25, OUT)
    c.dot(19, 23, HI)
    c.dot(18, 22, OUT)
    c.dot(20, 24, OUT)

    # решётка
    for y in (24, 26):
        c.hline(10, 15, y, DARK)

    # излучатели и помеха
    c.vline(10, 5, 10, OUT)
    c.dot(10, 4, AMBER)
    c.vline(21, 5, 10, OUT)
    c.dot(21, 4, RED)
    for row, points in ((8, [(12, 8), (13, 7), (14, 8), (15, 7), (16, 8), (17, 7), (18, 8), (19, 7)]),
                        (6, [(12, 6), (13, 5), (14, 6), (15, 5), (16, 6), (17, 5), (18, 6), (19, 5)])):
        for index, (x, y) in enumerate(points):
            if (index + row) % 3 != 0:
                c.dot(x, y, PURPLE)


def mj_north(c: Cell) -> None:
    """Глушитель сзади: глухой корпус, рёбра, кабель питания."""
    base(c)

    c.rect(7, 11, 24, 27, DARK)
    c.frame(7, 11, 24, 27, OUT)
    c.vline(8, 12, 26, LIT)
    c.vline(23, 12, 26, OUT)

    for y in range(13, 26, 3):
        c.hline(10, 21, y, MID)
        c.hline(10, 21, y + 1, OUT)

    c.poly([(19, 12), (20, 17), (19, 22), (20, 27)], COPPER)
    c.poly([(21, 13), (22, 18), (21, 23), (20, 28)], CABLE)
    c.hline(18, 22, 16, OUT)

    c.rect(11, 21, 15, 25, OUT)
    c.rect(12, 22, 14, 24, HI)


def mj_specific_south(c: Cell) -> None:
    """Глушитель конкретной частоты: вместо регулятора — дисплей с частотой."""
    mj_south(c)

    # стереть регулятор и нарисовать частотный дисплей
    c.rect(16, 20, 22, 26, PANEL)
    c.rect(16, 20, 21, 25, SCREEN)
    c.frame(16, 20, 21, 25, OUT)
    # три знака частоты
    for x in (17, 19, 20):
        c.vline(x, 21, 24, PURPLE)
    c.dot(18, 21, PURPLE)
    c.dot(18, 24, PURPLE)


def mj_side(c: Cell, mirror: bool) -> None:
    """Глушитель сбоку: узкий корпус, полоса предупреждения, излучатель."""
    base(c)

    c.rect(11, 11, 21, 27, DARK)
    c.frame(11, 11, 21, 27, OUT)
    c.vline(12, 12, 26, MID)
    c.vline(20, 12, 26, OUT)
    c.hline(12, 20, 11, LIT)

    # полоса предупреждения на боковине
    for y in range(12, 15):
        for x in range(12, 21):
            c.dot(x, y, AMBER if ((x + y) // 2) % 2 == 0 else OUT)

    # передняя кромка
    c.vline(13, 16, 26, PANEL)
    c.vline(14, 16, 26, PANEL)
    c.dot(14, 18, RED)
    c.dot(14, 23, GREEN)

    # излучатель
    c.vline(16, 5, 10, OUT)
    c.dot(16, 4, AMBER)

    # кабели
    c.poly([(22, 16), (23, 21), (22, 27)], COPPER)
    c.hline(21, 23, 19, OUT)

    if mirror:
        c.img = c.img.transpose(Image.FLIP_LEFT_RIGHT)
        c.px = c.img.load()


def tw_south(c: Cell) -> None:
    """Вышка спереди: решётчатая мачта, маяк на верхушке, кабинет внизу."""
    base(c)

    # кабинет
    c.rect(8, 22, 23, 30, MID)
    c.frame(8, 22, 23, 30, OUT)
    c.hline(9, 22, 22, LIT)
    c.hline(9, 22, 30, DARK)

    # дверца кабинета
    c.rect(10, 24, 16, 29, MID)
    c.frame(10, 24, 16, 29, OUT)
    c.dot(15, 27, HI)
    c.dot(19, 23, PURPLE)

    # вентиляция
    for y in (24, 26, 28):
        c.hline(18, 22, y, DARK)

    # мачта: решётка снизу, сплошная головка сверху
    lattice(c, 8, 22, 13, 18, 10, 21, step=5)
    c.rect(14, 3, 17, 8, DARK)
    c.frame(14, 3, 17, 8, OUT)
    c.vline(15, 4, 7, MID)

    # маяк на верхушке
    c.vline(15, 1, 2, OUT)
    c.dot(15, 0, AMBER)

    # тарелка передатчика
    c.rect(17, 5, 20, 8, MID)
    c.frame(17, 5, 20, 8, OUT)
    c.dot(18, 6, HI)


def tw_north(c: Cell) -> None:
    """Вышка сзади: та же решётка, кабель-канал и разъём питания."""
    base(c)

    c.rect(8, 22, 23, 30, MID)
    c.frame(8, 22, 23, 30, OUT)
    c.hline(9, 22, 22, LIT)
    c.hline(9, 22, 30, DARK)

    lattice(c, 8, 22, 13, 18, 10, 21, step=5)
    c.rect(14, 3, 17, 8, DARK)
    c.frame(14, 3, 17, 8, OUT)

    # кабель по мачте в кабинет
    c.poly([(16, 4), (19, 10), (20, 16), (18, 22)], COPPER)
    c.hline(17, 21, 27, OUT)
    c.rect(17, 24, 20, 28, OUT)
    c.rect(18, 25, 19, 27, HI)


def tw_side(c: Cell, mirror: bool) -> None:
    """Вышка сбоку: узкий кабинет, мачта с подсветкой грани, маяк."""
    base(c)

    c.rect(11, 22, 20, 30, DARK)
    c.frame(11, 22, 20, 30, OUT)
    c.hline(12, 19, 22, LIT)
    c.vline(12, 23, 29, MID)

    # дверца в профиль
    c.vline(13, 24, 29, PANEL)
    c.vline(14, 24, 29, PANEL)

    # мачта в профиль
    c.rect(14, 3, 17, 22, DARK)
    c.frame(14, 3, 17, 22, OUT)
    c.vline(15, 4, 21, MID)
    for y in range(6, 22, 4):
        c.hline(14, 17, y, OUT)

    c.vline(15, 0, 3, OUT)
    c.dot(15, 0, AMBER)

    if mirror:
        c.img = c.img.transpose(Image.FLIP_LEFT_RIGHT)
        c.px = c.img.load()


def paint(front, back, east, west) -> Image.Image:
    """Собрать полосу 128x32 из четырёх painters: S, N, E, W."""
    sheet = Image.new("RGBA", (SIZE * 4, SIZE), TRANSPARENT)
    for index, painter in enumerate((front, back, east, west)):
        cell = Cell()
        painter(cell)
        sheet.paste(cell.img, (index * SIZE, 0), cell.img)
    return sheet


def write_meta(root: Path, rsi: str, states: list[str]) -> None:
    meta = {
        'version': 1,
        'size': {'x': SIZE, 'y': SIZE},
        'states': [{'name': name, 'directions': 4} for name in states],
    }
    target = root / 'Resources' / 'Textures' / '_UU' / rsi / 'meta.json'
    target.write_text(json.dumps(meta, indent=2) + chr(10), encoding='utf-8')
    print(f'мета {rsi}: {states}')


def write(root: Path, rsi: str, state: str, painters, preview: bool) -> None:
    sheet = paint(*painters)
    target = root / "Resources" / "Textures" / "_UU" / rsi / f"{state}.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(target)
    print(f"записано {target.name} -> {rsi} ({sheet.width}x{sheet.height})")

    if preview:
        out = root / "sprites_preview" / f"{rsi.replace('.rsi', '')}_{state}_6x.png"
        out.parent.mkdir(parents=True, exist_ok=True)
        sheet.resize((sheet.width * 6, sheet.height * 6), Image.NEAREST).save(out)


def main() -> None:
    preview = "--preview" in sys.argv
    root = Path(__file__).resolve().parent.parent

    write(root, "transmitter.rsi", "icon",
          (south, north, tx_side, lambda c: tx_side(c, mirror=True)), preview)
    write(root, "jammer.rsi", "icon",
          (mj_south, mj_north, lambda c: mj_side(c, mirror=False), lambda c: mj_side(c, mirror=True)), preview)
    write(root, "jammer.rsi", "specific",
          (mj_specific_south, mj_north, lambda c: mj_side(c, mirror=False), lambda c: mj_side(c, mirror=True)), preview)
    write(root, "signal_tower.rsi", "icon",
          (tw_south, tw_north, lambda c: tw_side(c, mirror=False), lambda c: tw_side(c, mirror=True)), preview)

    write_meta(root, "jammer.rsi", ["icon", "specific"])
    write_meta(root, "signal_tower.rsi", ["icon"])


if __name__ == "__main__":
    main()
