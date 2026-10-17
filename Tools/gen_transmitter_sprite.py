"""Генератор спрайта радиочастотного ретранслятора (_UU/transmitter.rsi).

Рисует 32x32 пиксель-арт в четырёх направлениях (South, North, East, West) так,
как их ожидает Robust RSI: горизонтальная полоса 128x32, по одной 32x32 ячейке
на направление, порядок RsiDirection = South, North, East, West.

Запуск:
    python Tools/gen_transmitter_sprite.py            # записать в RSI
    python Tools/gen_transmitter_sprite.py --preview  # плюс превью 6x
"""
from __future__ import annotations

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


def side(c: Cell, mirror: bool) -> None:
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


def main() -> None:
    preview = "--preview" in sys.argv

    cells = []
    front = Cell()
    south(front)
    cells.append(front)

    back = Cell()
    north(back)
    cells.append(back)

    east = Cell()
    side(east, mirror=False)
    cells.append(east)

    west = Cell()
    side(west, mirror=True)
    cells.append(west)

    sheet = Image.new("RGBA", (SIZE * len(cells), SIZE), TRANSPARENT)
    for index, cell in enumerate(cells):
        sheet.paste(cell.img, (index * SIZE, 0), cell.img)

    root = Path(__file__).resolve().parent.parent
    target = root / "Resources" / "Textures" / "_UU" / "transmitter.rsi" / "icon.png"
    sheet.save(target)
    print(f"записано {target} ({sheet.width}x{sheet.height})")

    if preview:
        out = root / "transmitter_preview_6x.png"
        sheet.resize((sheet.width * 6, sheet.height * 6), Image.NEAREST).save(out)
        print(f"превью {out}")


if __name__ == "__main__":
    main()
