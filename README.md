# Unknown Union (UU)

Русскоязычный RP-сервер на базе Space Station 14. Исторически это форк
[Corvax](https://github.com/space-syndicate/space-station-14) (ответвление
[Space Station 14](https://github.com/space-wizards/space-station-14)) на движке
[RobustToolbox](https://github.com/space-wizards/RobustToolbox) версии 293.0.1.

Контент дальше развивается самостоятельно: UU — отдельный сервер, а не ветка Corvax.
Из апстрима тянем **только движок** (сабмодуль `RobustToolbox`), контент и прототипы —
свои. Поэтому спорные апстримные решения (например, департаментские радиоканалы) у нас
не восстанавливаются: расхождения с апстримом — это нормальное состояние репозитория.

**Сектор UU141, планета Аксис.** Геополитическая RP-песочница: государства и их границы,
документы, подписи и архивы, союзы и предательства, колонисты и экономика. Бумага — это власть.
RP здесь обязателен; токсичность и «просто пострелять» — не сюда.

## Чем этот репозиторий отличается от апстрима

Собственный контент лежит в `_UU`-папках и не пересекается с апстримом:

| Система | Где |
| --- | --- |
| Радиосвязь на частотах: передатчики, рации, гарнитуры, конфигуратор частот, сигнальные вышки с кодом доступа, глушители | `Content.Server/_UU/Radio`, `Content.Client/_UU/Radio`, `Content.Shared/_UU/Radio` |
| Фракции | `Content.Server/_UU/FactionSystem`, `Content.Shared/_UU/Faction` |
| Ячейки и назначения | `Content.Server/_UU/CellSystem`, `Content.Client/_UU/Cell`, `Content.Shared/_UU/Cell`, `Content.Server/_UU/AppointmentsSystem` |
| FPV-дрон | `Content.Server/_UU/FantasikTest` |
| Рыбалка | `Content.Server/_UU/FishingRod`, `Content.Shared/_UU/FishingRod` |
| Инструмент предпросмотра XAML-окон | `Content.Client/_UU/UiPreview` |

Плюс слой Corvax, унаследованный от форка: TTS-озвучка (`Content.Server/Corvax/TTS`), IPC,
окно службы безопасности, цели станции, генератор гайдбука, административные документы.

Прототипы — `Resources/Prototypes/_UU`, локализация — `Resources/Locale/{ru-RU,en-US}/_UU`,
спрайты — `Resources/Textures/_UU`.

### Радиосистема коротко

- Частоты свободные (1–3 символа), передача — `:ЧАСТОТА` в чате. Департаментских каналов нет.
- Передатчик задаёт частоту один раз; конфигуратор частот копирует её и раздаёт по устройствам.
- Сигнальные вышки ретранслируют частоту по цепочке. Без питания вышка не работает, а код доступа
  ограничивает круг тех, кто может её перенастроить.
- Глушитель настраивается только конфигуратором. Глушение бьёт по источнику: накрытый помехой
  передатчик не излучает вообще, накрытая вышка рвёт цепочку за собой.

## Требования

- [.NET SDK 10.0.100](https://dotnet.microsoft.com/) (`global.json` разрешает обновления в пределах 10.0.x)
- Git с поддержкой сабмодулей
- Python 3.5+ — для `RUN_THIS.py`
- Docker — только если нужен TTS (контейнер `ss14tts`)

## Сборка и запуск

```bash
git clone --recursive https://github.com/KtoIaSharp/Unknown-Union_SS14.git
cd Unknown-Union_SS14
python RUN_THIS.py          # инициализация сабмодулей и движка
dotnet build                # или run-build-debug.bat
```

Дальше:

- `run-server-debug.bat` — сервер
- `run-client-debug.bat` — клиент
- `duo.bat` — сервер и два клиента сразу (плюс TTS, если поднят Docker)
- `tts_start.bat` — поднять контейнер TTS

**Собирайте в Debug.** Конфигурация Release сейчас падает на анализаторах по всему проекту
(`RA0049`/`RA0051`: `[Dependency]`-поля объявлены `readonly`, системы не помечены `partial`).
Это известно и не связано с `_UU`-кодом.

### Конфигурация сервера

Пресеты — `Resources/ConfigPresets/Corvax/`:

- `common.toml` — `tickrate = 30`
- `main.toml` — мейн, 190 слотов (soft cap 105)
- `wl.toml` — вайтлист, 80 слотов
- `elysium.toml` — Элизиум, 180 слотов
- `mrp.toml` — MRP

Пресеты всё ещё несут брендинг Corvax (`hostname`, `lobby_name`) — это остаток форка, а не UU.
Правится там же, где и остальные настройки сервера.

## Проверки перед коммитом

```bash
dotnet build Content.Server/Content.Server.csproj -c Debug
dotnet build Content.Client/Content.Client.csproj -c Debug
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c Debug
```

YAML-линтер — авторитет по `!type:` и полям C#-компонентов: он ловит опечатки в прототипах,
которых не видит обычная сборка.

### Предпросмотр окон без запуска игры

```bash
dotnet run --project Content.Client/Content.Client.csproj -c Debug --no-build -- \
  --cvar uu.ui_preview.enabled=true \
  --cvar uu.ui_preview.windows=Content.Client._UU.Radio.TowerWindow \
  --cvar uu.ui_preview.quit_after=true
```

PNG-ки складываются в `%APPDATA%\Space Station 14\data\UiPreview`. Инструмент рендерит окно
настоящим клиентом — со стилями, шрифтами и RSI, — и обрезает кадр по самому окну.

## Обновление движка

Движок обновляется одним скриптом — он подтягивает сабмодуль, заново накатывает нашу
правку `ReflectionManager`, пересобирает и прогоняет YAML-линтер:

```powershell
pwsh Tools/update_engine.ps1                 # до текущего origin/master движка
pwsh Tools/update_engine.ps1 -Ref v293.0.1   # до конкретного тега
```

Если PowerShell 7 не установлен, тот же скрипт запускается из Windows PowerShell 5.1:
`powershell -NoProfile -ExecutionPolicy Bypass -File Tools\update_engine.ps1 -Ref v293.0.1`.

Если патч не ложится — движок переписал код вокруг него. Скрипт остановится и скажет,
что делать: перенести null-guard в новое место и перевыпустить
`Tools/patches/robust-reflectionmanager-null-guard.patch`. Сборка в этом случае не запускается.

После обновления прогоните сервер хотя бы на минуту и посмотрите на `[ERRO]` в логе:
часть поломок (например, потерянные регистрации в IoC) видна только в рантайме, а не при сборке.

## Инструменты

- `Tools/update_engine.ps1` — обновление движка (см. выше)
- `Tools/gen_radio_sprites.py` — генератор спрайтов радио-набора (4 направления, `meta.json`),
  `--preview` пишет увеличенные превью в `sprites_preview/`
- `Tools/fix_rsis.py`, `Tools/sync_textures.py` — массовая починка и синхронизация RSI
- `Tools/patches/robust-reflectionmanager-null-guard.patch` — единственная правка движка
  (null-guard в `ReflectionManager`); после обновления сабмодуля патч надо накатить заново

## Лицензия и авторство

Код репозитория — [MIT](LICENSE.TXT). Большинство ассетов — [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/),
если в `meta.json` не указано иное; часть ассетов некоммерческая (CC-BY-NC-SA 3.0 и аналогичные) —
их нужно удалить, если планируется коммерческое использование.

Это форк: значительная часть кода, контента и спрайтов написана командами
[Space Wizards](https://github.com/space-wizards) и [Corvax](https://github.com/space-syndicate).
При переносе чужого контента сохраняйте лицензии в `meta.json` и указывайте авторство.

## Документация

- [Документация SS14](https://docs.spacestation14.io/) — контент, движок, дизайн, ECS
- [Руководство по PR](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html)

## Известные проблемы

- Release-сборка не проходит из-за анализаторов (см. выше) — используйте Debug.
- Ручной `RadioJammer` из апстрима всё ещё глушит удалённые департаментские каналы, то есть
  фактически бесполезен и дублирует стационарные глушители. Решение по нему не принято.
- У окна `SecApartmentWindow.xaml` четыре незарегистрированных style-класса, из-за чего оно
  выглядит не так, как задумано.
- Гайдбук-запись по радиосистеме пока не написана.
