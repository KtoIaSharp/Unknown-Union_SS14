"""Генератор арта для трудовых документов UU.

Пишет RSI в Resources/Textures/_UU/:
  work_id.rsi     — пластиковое удостоверение о работе (icon, inhand-left, inhand-right)
  work_writer.rsi — ручной работодатель, сканер-записыватель (icon, inhand-left, inhand-right)

Палитра совпадает с остальным артом UU (см. Tools/gen_radio_sprites.py):
тёмная индустриальная основа, фиолетовый акцент, тёплые индикаторы.
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
SCREEN = (34, 52, 62, 255)
CARD = (176, 188, 208, 255)
CARD_DARK = (128, 142, 165, 255)
PHOTO = (86, 98, 116, 255)
GREEN = (99, 212, 113, 255)
AMBER = (255, 179, 71, 255)
PURPLE = (138, 108, 240, 255)
CABLE = (17, 19, 23, 255)


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


def draw_work_id(scale: int = 1) -> Canvas:
    """Пластиковая карточка водительского типа."""
    c = Canvas()
    if scale == 1:
        # корпус карты
        c.rect(5, 9, 26, 23, CARD)
        c.frame(5, 9, 26, 23, OUT)
        c.hline(6, 25, 9, HI)
        # фотография владельца
        c.rect(8, 12, 13, 18, PHOTO)
        c.frame(8, 12, 13, 18, CARD_DARK)
        c.dot(10, 14, CARD)
        c.dot(11, 14, CARD)
        c.hline(9, 12, 16, CARD)
        c.hline(9, 12, 17, CARD)
        # строки данных
        c.hline(16, 24, 12, CARD_DARK)
        c.hline(16, 22, 14, CARD_DARK)
        c.hline(16, 24, 16, CARD_DARK)
        # фиолетовая полоса брендинга и подпись владельца
        c.hline(16, 24, 18, PURPLE)
        c.dot(16, 20, CARD_DARK)
        c.dot(18, 19, CARD_DARK)
        c.dot(20, 20, CARD_DARK)
        c.dot(22, 19, CARD_DARK)
        # магнитная полоса
        c.hline(6, 25, 21, MID)
        c.hline(6, 25, 22, CARD_DARK)
    else:
        # уменьшенная версия для руки
        c.rect(9, 12, 22, 20, CARD)
        c.frame(9, 12, 22, 20, OUT)
        c.rect(11, 14, 14, 18, PHOTO)
        c.hline(16, 20, 14, CARD_DARK)
        c.hline(16, 20, 16, CARD_DARK)
        c.hline(16, 20, 17, PURPLE)
        c.hline(10, 21, 19, MID)
    return c


def draw_work_writer(scale: int = 1) -> Canvas:
    """Ручной работодатель: сканер-записыватель данных о работе."""
    c = Canvas()
    if scale == 1:
        # корпус
        c.rect(7, 6, 24, 22, DARK)
        c.frame(7, 6, 24, 22, OUT)
        c.hline(8, 23, 7, MID)
        # экран
        c.rect(10, 9, 21, 15, SCREEN)
        c.frame(10, 9, 21, 15, PANEL)
        c.hline(12, 19, 11, GREEN)
        c.hline(12, 17, 13, GREEN)
        # сканер-щель
        c.rect(11, 17, 20, 18, PANEL)
        c.hline(12, 19, 17, PURPLE)
        # кнопки
        c.dot(19, 20, GREEN)
        c.dot(22, 20, AMBER)
        # рукоять
        c.rect(11, 22, 14, 27, MID)
        c.frame(11, 22, 14, 27, OUT)
        c.vline(12, 23, 26, LIT)
        # кабель снизу
        c.vline(10, 27, 29, CABLE)
        c.dot(9, 29, CABLE)
        # фиолетовая полоса брендинга
        c.hline(8, 10, 7, PURPLE)
    else:
        c.rect(11, 9, 22, 19, DARK)
        c.frame(11, 9, 22, 19, OUT)
        c.rect(13, 11, 20, 15, SCREEN)
        c.hline(14, 18, 12, GREEN)
        c.hline(13, 21, 17, PURPLE)
        c.rect(13, 19, 15, 22, MID)
    return c


STATES = {
    "work_id": [("icon", lambda: draw_work_id(1)), ("inhand-left", lambda: draw_work_id(2)),
                ("inhand-right", lambda: draw_work_id(2))],
    "work_writer": [("icon", lambda: draw_work_writer(1)), ("inhand-left", lambda: draw_work_writer(2)),
                    ("inhand-right", lambda: draw_work_writer(2))],
}


def write_rsi(rsi: str, states, preview: bool) -> None:
    folder = TEXTURES / f"{rsi}.rsi"
    folder.mkdir(parents=True, exist_ok=True)

    meta = {"version": 1, "size": {"x": SIZE, "y": SIZE}, "states": [{"name": name} for name, _ in states]}
    (folder / "meta.json").write_text(json.dumps(meta, indent=2) + "\n", encoding="utf-8")

    sheet = Image.new("RGBA", (SIZE * len(states), SIZE), TRANSPARENT)
    for index, (name, painter) in enumerate(states):
        canvas = painter()
        canvas.image.save(folder / f"{name}.png")
        sheet.paste(canvas.image, (index * SIZE, 0))
        if preview:
            out = ROOT / "sprites_preview"
            out.mkdir(exist_ok=True)
            canvas.image.resize((SIZE * 6, SIZE * 6), Image.NEAREST).save(out / f"{rsi}_{name}_6x.png")

    print(f"written {folder.relative_to(ROOT)} ({len(states)} states)")


def main() -> int:
    preview = "--preview" in __import__("sys").argv
    for rsi, states in STATES.items():
        write_rsi(rsi, states, preview)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
