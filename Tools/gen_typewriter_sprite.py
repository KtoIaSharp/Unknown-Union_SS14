"""Печатная машинка UU: стационарная, с валиком и листом бумаги.

Пишет Resources/Textures/_UU/typewriter.rsi:
  icon  — пустая машинка
  paper — лист в валике

Палитра как у остальной техники UU (Tools/gen_radio_sprites.py).
"""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
TEXTURES = ROOT / "Resources" / "Textures" / "_UU"
SIZE = 32

TRANSPARENT = (0, 0, 0, 0)
OUT = (13, 15, 18, 255)
DARK = (30, 34, 41, 255)
MID = (47, 54, 64, 255)
LIT = (73, 83, 97, 255)
HI = (104, 116, 134, 255)
PANEL = (22, 26, 32, 255)
CARD = (196, 206, 222, 255)
CARD_DARK = (150, 162, 180, 255)
INK = (58, 62, 72, 255)
PURPLE = (138, 108, 240, 255)
AMBER = (255, 179, 71, 255)


class Canvas:
    def __init__(self) -> None:
        self.image = Image.new("RGBA", (SIZE, SIZE), TRANSPARENT)
        self.px = self.image.load()

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

    def hline(self, x0: int, x1: int, y: int, color) -> None:
        for x in range(x0, x1 + 1):
            self.dot(x, y, color)

    def vline(self, x: int, y0: int, y1: int, color) -> None:
        for y in range(y0, y1 + 1):
            self.dot(x, y, color)


def draw(with_paper: bool) -> Canvas:
    c = Canvas()

    # лист в валике (рисуем первым — валик перекрывает низ листа)
    if with_paper:
        c.rect(9, 1, 22, 11, CARD)
        c.frame(9, 1, 22, 11, CARD_DARK)
        c.hline(11, 20, 3, INK)
        c.hline(11, 18, 5, INK)
        c.hline(11, 20, 7, INK)
        c.hline(11, 16, 9, INK)

    # корпус
    c.rect(3, 17, 28, 26, DARK)
    c.frame(3, 17, 28, 26, OUT)
    c.hline(4, 27, 17, MID)
    # клавиши: три ряда
    for row, y in enumerate((20, 22, 24)):
        for x in range(6, 23, 3):
            c.rect(x, y, x + 1, y, LIT if (x + row) % 2 else HI)
    # фиолетовая полоса брендинга и индикатор
    c.rect(3, 18, 26, 18, PURPLE)
    c.dot(26, 20, AMBER)
    # возврат каретки
    c.vline(27, 13, 18, MID)
    c.hline(27, 29, 13, LIT)

    # валик
    c.rect(5, 12, 26, 16, MID)
    c.frame(5, 12, 26, 16, OUT)
    c.hline(6, 25, 13, LIT)
    c.hline(6, 25, 15, DARK)
    # ручки валика
    c.rect(2, 13, 4, 15, HI)
    c.frame(2, 13, 4, 15, OUT)
    c.rect(27, 13, 29, 15, HI)
    c.frame(27, 13, 29, 15, OUT)

    # ножки
    c.rect(5, 27, 7, 28, PANEL)
    c.rect(24, 27, 26, 28, PANEL)

    return c


STATES = {"icon": False, "paper": True}


def main() -> int:
    preview = "--preview" in __import__("sys").argv
    folder = TEXTURES / "typewriter.rsi"
    folder.mkdir(parents=True, exist_ok=True)

    meta = {
        "version": 1,
        "size": {"x": SIZE, "y": SIZE},
        "states": [{"name": name} for name in STATES],
    }
    (folder / "meta.json").write_text(json.dumps(meta, indent=2) + "\n", encoding="utf-8")

    out_dir = ROOT / "sprites_preview"
    if preview:
        out_dir.mkdir(exist_ok=True)

    for name, with_paper in STATES.items():
        canvas = draw(with_paper)
        canvas.image.save(folder / f"{name}.png")
        if preview:
            canvas.image.resize((SIZE * 6, SIZE * 6), Image.NEAREST).save(out_dir / f"typewriter_{name}_6x.png")

    print(f"written {folder.relative_to(ROOT)} ({len(STATES)} states)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
