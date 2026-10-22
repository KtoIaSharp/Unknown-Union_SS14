"""Иконки для наших вербов (меню взаимодействия).

Рисует 32x32 глифы в палитре UU: тёмная подложка не нужна — вербы показывают
иконку на своём фоне, поэтому фон прозрачный, глиф светлый, акцент фиолетовый.

Запуск: python Tools/gen_verb_icons.py [--preview]
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
TEXTURES = ROOT / "Resources" / "Textures" / "_UU"
PREVIEW = ROOT / "sprites_preview"

SIZE = 32

INK = (232, 236, 244, 255)
DIM = (140, 150, 166, 255)
PURPLE = (138, 108, 240, 255)
GOLD = (214, 178, 92, 255)
DARK = (26, 30, 38, 255)


class Canvas:
    def __init__(self) -> None:
        self.image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

    def dot(self, x: int, y: int, color) -> None:
        if 0 <= x < SIZE and 0 <= y < SIZE:
            self.image.putpixel((x, y), color)

    def rect(self, x0: int, y0: int, x1: int, y1: int, color) -> None:
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, color)

    def frame(self, x0: int, y0: int, x1: int, y1: int, color) -> None:
        self.rect(x0, y0, x1, y0, color)
        self.rect(x0, y1, x1, y1, color)
        self.rect(x0, y0, x0, y1, color)
        self.rect(x1, y0, x1, y1, color)

    def hline(self, x0: int, x1: int, y: int, color) -> None:
        self.rect(x0, y, x1, y, color)

    def vline(self, x: int, y0: int, y1: int, color) -> None:
        self.rect(x, y0, x, y1, color)


def draw_console(c: Canvas) -> None:
    """Консоль: монитор с экраном."""
    c.rect(5, 6, 26, 21, DARK)
    c.frame(5, 6, 26, 21, INK)
    c.rect(8, 9, 23, 18, PURPLE)
    c.hline(9, 15, 11, INK)
    c.hline(9, 13, 13, INK)
    c.hline(9, 16, 15, INK)
    c.rect(14, 22, 17, 24, INK)
    c.hline(9, 22, 26, INK)


def draw_bind(c: Canvas) -> None:
    """Закрепить за государством: флаг на древке."""
    c.vline(9, 4, 27, INK)
    c.rect(10, 5, 23, 14, PURPLE)
    c.frame(10, 5, 23, 14, INK)
    c.hline(12, 20, 8, INK)
    c.hline(12, 18, 11, INK)
    c.rect(6, 26, 12, 27, DARK)


def draw_card(c: Canvas) -> None:
    """Удостоверение: пластиковая карта."""
    c.rect(4, 8, 27, 23, DARK)
    c.frame(4, 8, 27, 23, INK)
    c.rect(6, 10, 12, 16, PURPLE)
    c.hline(14, 25, 11, INK)
    c.hline(14, 22, 14, DIM)
    c.hline(14, 24, 17, DIM)
    c.hline(6, 25, 21, PURPLE)


def draw_contract(c: Canvas) -> None:
    """Договор: лист с подписью."""
    c.rect(7, 4, 24, 27, DARK)
    c.frame(7, 4, 24, 27, INK)
    for y in (8, 11, 14):
        c.hline(10, 21, y, DIM)
    c.hline(10, 18, 18, DIM)
    c.hline(10, 20, 22, PURPLE)


def draw_pen(c: Canvas, seal: bool = False) -> None:
    """Подпись: ручка (для государства — с печатью)."""
    for i in range(12):
        c.rect(8 + i, 24 - i, 10 + i, 26 - i, INK)
    c.rect(19, 10, 23, 14, PURPLE)
    c.rect(6, 24, 10, 27, DARK)
    if seal:
        c.frame(18, 18, 27, 27, GOLD)
        c.dot(22, 22, GOLD)
        c.dot(23, 23, GOLD)


def draw_document(c: Canvas) -> None:
    """Документ: чистый лист."""
    c.rect(8, 4, 23, 27, DARK)
    c.frame(8, 4, 23, 27, INK)
    c.rect(11, 8, 20, 9, PURPLE)
    c.hline(11, 20, 13, DIM)
    c.hline(11, 18, 16, DIM)
    c.hline(11, 20, 19, DIM)
    c.hline(11, 16, 22, DIM)


def draw_invite(c: Canvas) -> None:
    """Приглашение: конверт."""
    c.rect(5, 9, 26, 22, DARK)
    c.frame(5, 9, 26, 22, INK)
    for i in range(9):
        c.dot(6 + i, 10 + i, DIM)
        c.dot(25 - i, 10 + i, DIM)
    c.hline(11, 20, 16, PURPLE)
    c.hline(13, 18, 18, PURPLE)


def draw_rename(c: Canvas) -> None:
    """Переименовать: карандаш по листу."""
    c.rect(5, 6, 18, 26, DARK)
    c.frame(5, 6, 18, 26, INK)
    c.hline(8, 15, 10, DIM)
    c.hline(8, 13, 13, DIM)
    for i in range(11):
        c.rect(14 + i, 25 - i, 17 + i, 28 - i, GOLD)
    c.rect(24, 14, 27, 17, PURPLE)


def draw_disband(c: Canvas) -> None:
    """Распустить: печать с крестом."""
    c.frame(6, 6, 25, 25, GOLD)
    c.rect(6, 6, 25, 8, DARK)
    for i in range(13):
        c.dot(10 + i, 11 + i, INK)
        c.dot(21 - i, 11 + i, INK)


STATES = {
    "open_console": draw_console,
    "bind_faction": draw_bind,
    "open_card": draw_card,
    "open_contract": draw_contract,
    "sign_personal": lambda c: draw_pen(c, seal=False),
    "sign_state": lambda c: draw_pen(c, seal=True),
    "document": draw_document,
    "invite": draw_invite,
    "rename": draw_rename,
    "disband": draw_disband,
}


def main() -> None:
    preview = "--preview" in sys.argv

    folder = TEXTURES / "verb_icons.rsi"
    folder.mkdir(parents=True, exist_ok=True)

    meta = {"version": 1, "size": {"x": SIZE, "y": SIZE}, "states": []}
    PREVIEW.mkdir(exist_ok=True)

    for name, painter in STATES.items():
        canvas = Canvas()
        painter(canvas)
        canvas.image.save(folder / f"{name}.png")
        meta["states"].append({"name": name})
        print(f"written {folder / (name + '.png')}")

        if preview:
            big = canvas.image.resize((SIZE * 6, SIZE * 6), Image.NEAREST)
            background = Image.new("RGBA", big.size, (24, 27, 34, 255))
            background.alpha_composite(big)
            background.save(PREVIEW / f"verb_{name}_6x.png")

    (folder / "meta.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    print("states:", [s["name"] for s in meta["states"]])


if __name__ == "__main__":
    main()
