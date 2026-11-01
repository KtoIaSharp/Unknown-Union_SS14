"""РљРѕРЅСЃРѕР»СЊ СѓРїСЂР°РІР»РµРЅРёСЏ РґРѕР»Р¶РЅРѕСЃС‚СЏРјРё UU: 32x32, С‡РµС‚С‹СЂРµ РЅР°РїСЂР°РІР»РµРЅРёСЏ (СЋРі/СЃРµРІРµСЂ/РІРѕСЃС‚РѕРє/Р·Р°РїР°Рґ).

Р Р°СЃРєР»Р°РґРєР° РїРѕР»РѕСЃС‹: RsiDirection South=0, North=1, East=2, West=3.
Р—Р°РїСѓСЃРє: python Tools/gen_work_console_sprite.py --preview
"""
import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path.cwd()

OUT = (13, 15, 18)
DARK = (30, 34, 41)
MID = (47, 54, 64)
LIT = (73, 83, 97)
HI = (104, 116, 134)
PANEL = (22, 26, 32)
SCREEN = (34, 52, 62)
GREEN = (99, 212, 113)
AMBER = (255, 179, 71)
PURPLE = (138, 108, 240)


class Cell:
    def __init__(self):
        self.px = [[None] * 32 for _ in range(32)]

    def dot(self, x, y, c):
        if 0 <= x < 32 and 0 <= y < 32:
            self.px[y][x] = c

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, c)

    def hline(self, x0, x1, y, c):
        self.rect(x0, y, x1, y, c)

    def vline(self, x, y0, y1, c):
        self.rect(x, y0, x, y1, c)

    def image(self):
        img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        for y in range(32):
            for x in range(32):
                if self.px[y][x] is not None:
                    img.putpixel((x, y), (*self.px[y][x], 255))
        return img


def base(c):
    # РџР»РёС‚Р°-РѕСЃРЅРѕРІР°РЅРёРµ Рё РЅРѕР¶РєРё.
    c.rect(5, 26, 26, 29, LIT)
    c.rect(5, 29, 26, 30, DARK)
    c.rect(4, 26, 4, 30, OUT)
    c.rect(27, 26, 27, 30, OUT)
    c.hline(5, 26, 26, HI)
    c.rect(7, 30, 10, 31, DARK)
    c.rect(21, 30, 24, 31, DARK)


def south(c):
    base(c)
    # РљРѕСЂРїСѓСЃ.
    c.rect(8, 8, 23, 26, DARK)
    c.rect(8, 8, 8, 26, OUT)
    c.rect(23, 8, 23, 26, OUT)
    c.hline(9, 22, 8, MID)
    # СЂР°РјРєР° СЌРєСЂР°РЅР°
    c.rect(10, 10, 21, 20, PANEL)
    c.rect(11, 11, 20, 18, SCREEN)
    # СЃС‚СЂРѕРєРё РЅР° СЌРєСЂР°РЅРµ
    c.hline(12, 18, 12, PURPLE)
    c.hline(12, 19, 13, LIT)
    c.hline(12, 17, 14, LIT)
    c.hline(12, 16, 15, MID)
    c.dot(13, 17, GREEN)
    c.dot(15, 17, AMBER)
    # С„РёРѕР»РµС‚РѕРІР°СЏ РїРѕР»РѕСЃР° Р±СЂРµРЅРґРёРЅРіР°
    c.hline(9, 22, 20, PURPLE)
    # РєР»Р°РІРёР°С‚СѓСЂР°
    c.rect(9, 22, 22, 25, MID)
    for x in range(10, 22, 2):
        c.dot(x, 23, HI)
        c.dot(x + 1, 24, PANEL)
    # РёРЅРґРёРєР°С‚РѕСЂ РїРёС‚Р°РЅРёСЏ
    c.dot(21, 21, GREEN)


def north(c):
    base(c)
    c.rect(8, 8, 23, 26, DARK)
    c.rect(8, 8, 8, 26, OUT)
    c.rect(23, 8, 23, 26, OUT)
    c.hline(9, 22, 8, MID)
    # РІРµРЅС‚РёР»СЏС†РёСЏ РЅР° Р·Р°РґРЅРµР№ РїР°РЅРµР»Рё
    for y in range(11, 22, 2):
        c.hline(11, 20, y, PANEL)
        c.hline(11, 20, y + 1, MID)
    # РєР°Р±РµР»СЊ РїРёС‚Р°РЅРёСЏ
    c.vline(16, 26, 31, OUT)
    c.vline(17, 26, 30, PANEL)
    c.dot(21, 24, AMBER)


def side(c, mirror):
    base(c)
    # РЈР·РєРёР№ РєРѕСЂРїСѓСЃ СЃР±РѕРєСѓ.
    c.rect(10, 9, 21, 26, DARK)
    c.rect(10, 9, 10, 26, OUT)
    c.rect(21, 9, 21, 26, OUT)
    c.rect(12, 11, 19, 19, PANEL)
    # РєСЂРѕРјРєР° СЌРєСЂР°РЅР° РІРёРґРЅР° РїРѕРґ СѓРіР»РѕРј
    c.rect(13, 12, 15, 18, SCREEN)
    c.vline(16, 12, 18, PURPLE)
    c.rect(17, 12, 19, 18, LIT)
    c.hline(11, 20, 20, PURPLE)
    # РєР»Р°РІРёС€Рё РІ РїРµСЂСЃРїРµРєС‚РёРІРµ
    for y in range(22, 26):
        c.hline(12, 19, y, MID)
    c.dot(19, 23, HI)
    c.dot(18, 25, GREEN)
    if mirror:
        flipped = Cell()
        for y in range(32):
            for x in range(32):
                flipped.px[y][31 - x] = c.px[y][x]
        return flipped
    return c


def write(rsi, preview):
    rsi_dir = ROOT / "Resources" / "Textures" / "_UU" / (rsi + ".rsi")
    rsi_dir.mkdir(parents=True, exist_ok=True)

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

    eastmirror = Cell()
    side(eastmirror, mirror=True)
    cells.append(eastmirror)

    strip = Image.new("RGBA", (128, 32), (0, 0, 0, 0))
    for i, cell in enumerate(cells):
        strip.paste(cell.image(), (i * 32, 0))
    strip.save(rsi_dir / "icon.png")

    meta = {
        "version": 1,
        "size": {"x": 32, "y": 32},
        "states": [{"name": "icon", "directions": 4}],
    }
    (rsi_dir / "meta.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")

    if preview:
        prev_dir = ROOT / "sprites_preview"
        prev_dir.mkdir(exist_ok=True)
        strip.resize((768, 192), Image.NEAREST).save(prev_dir / f"{rsi}_icon_6x.png")

    print(f"written Resources\\Textures\\_UU\\{rsi}.rsi (icon, directions: 4)")


if __name__ == "__main__":
    write("work_console", "--preview" in sys.argv)

