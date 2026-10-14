import os
import sys
import shutil

def sync_textures(source_dir, target_dir, dry_run=False):
    source_dir = os.path.abspath(source_dir)
    target_dir = os.path.abspath(target_dir)

    if not os.path.isdir(source_dir):
        print(f"Ошибка: папка-источник не найдена: {source_dir}")
        return
    if not os.path.isdir(target_dir):
        print(f"Ошибка: папка-цель не найдена: {target_dir}")
        return

    replaced = 0
    skipped = 0

    for root, dirs, files in os.walk(source_dir):
        for file in files:
            rel_path = os.path.relpath(os.path.join(root, file), source_dir)
            target_path = os.path.join(target_dir, rel_path)

            if os.path.exists(target_path):
                if dry_run:
                    print(f"[DRY-RUN] Будет заменён: {rel_path}")
                else:
                    os.makedirs(os.path.dirname(target_path), exist_ok=True)
                    shutil.copy2(os.path.join(root, file), target_path)
                    print(f"Заменён: {rel_path}")
                replaced += 1
            else:
                skipped += 1

    print(f"\nГотово. Заменено: {replaced}, пропущено (нет в целевой папке): {skipped}")


if __name__ == "__main__":
    print("=== Синхронизация текстур ===")
    source = input("Папка ОТКУДА (новые файлы): ").strip().strip('"')
    target = input("Папка КУДА (заменить): ").strip().strip('"')
    dry = input("Сухой прогон? (y/n, по умолчанию y): ").strip().lower()

    sync_textures(source, target, dry_run=(dry != "n"))

    input("\nНажми Enter чтобы закрыть...")
