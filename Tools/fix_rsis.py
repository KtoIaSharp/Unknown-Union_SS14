import os
import json

textures_dir = r"C:\Users\user\Desktop\SS14Pro!ect\UU\Resources\Textures"

fixed = 0

for root, dirs, files in os.walk(textures_dir):
    if "meta.json" not in files:
        continue

    meta_path = os.path.join(root, "meta.json")

    with open(meta_path, "r", encoding="utf-8") as f:
        try:
            meta = json.load(f)
        except json.JSONDecodeError:
            continue

    if "states" not in meta:
        continue

    # Собираем реальные PNG-файлы
    existing_pngs = set()
    for fn in os.listdir(root):
        if fn.lower().endswith(".png"):
            existing_pngs.add(os.path.splitext(fn)[0])

    # Проверяем states
    original_states = list(meta["states"])
    new_states = [s for s in original_states if s["name"] in existing_pngs]

    if len(new_states) != len(original_states):
        removed = [s["name"] for s in original_states if s["name"] not in existing_pngs]
        meta["states"] = new_states
        with open(meta_path, "w", encoding="utf-8") as f:
            json.dump(meta, f, indent=4)
        print(f"Исправлен: {os.path.relpath(meta_path, textures_dir)} — удалено {len(removed)} состояний: {', '.join(removed[:5])}")
        fixed += 1

print(f"\nГотово. Исправлено meta.json: {fixed}")
input("\nНажми Enter...")
