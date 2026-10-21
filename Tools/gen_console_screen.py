"""Экран консоли работодателя: полупрозрачная накладка на спрайт компьютера."""

from pathlib import Path
import json

from PIL import Image

OUT = Path(__file__).resolve().parent.parent / "Resources/Textures/_UU/work_console.rsi"

PURPLE = (138, 108, 240, 255)
PURPLE_DIM = (86, 66, 150, 255)
GREEN = (99, 212, 113, 255)
PANEL = (16, 18, 24, 210)
OUTLINE = (60, 66, 78, 255)
LIGHT = (140, 150, 166, 255)


def make() -> Image.Image:
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    px = img.load()

    def dot(x: int, y: int, color) -> None:
        if 0 <= x < 32 and 0 <= y < 32:
            px[x, y] = color

    def rect(x0: int, y0: int, x1: int, y1: int, color) -> None:
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                dot(x, y, color)

    # панель экрана
    rect(6, 8, 25, 21, PANEL)

    # рамка
    for x in range(6, 26):
        dot(x, 8, OUTLINE)
        dot(x, 21, OUTLINE)
    for y in range(8, 22):
        dot(6, y, OUTLINE)
        dot(25, y, OUTLINE)

    # строка заголовка
    rect(8, 10, 16, 10, PURPLE)
    dot(18, 10, PURPLE_DIM)
    dot(19, 10, PURPLE_DIM)

    # строки полей
    rect(8, 13, 14, 13, LIGHT)
    rect(16, 13, 22, 13, PURPLE_DIM)
    rect(8, 15, 12, 15, LIGHT)
    rect(14, 15, 22, 15, PURPLE_DIM)

    # индикаторы
    dot(8, 18, GREEN)
    dot(10, 18, PURPLE)
    dot(12, 18, PURPLE_DIM)

    # блик на стекле
    for i in range(4):
        dot(7 + i, 9 + i, (200, 210, 230, 60))

    return img


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    img = make()
    img.save(OUT / "screen.png")

    meta = {
        "version": 1,
        "size": {"x": 32, "y": 32},
        "states": [{"name": "screen"}],
    }
    (OUT / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")

    preview = Path("sprites_preview")
    preview.mkdir(exist_ok=True)
    img.resize((192, 192), Image.NEAREST).save(preview / "work_console_screen_6x.png")
    print("written", OUT / "screen.png")


if __name__ == "__main__":
    main()
