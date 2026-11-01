ent-PhotoCamera = фотоаппарат
    .desc = Простой фотоаппарат для паспортных снимков на удостоверение.

ent-WorkPhoto = фотография
    .desc = Портретная фотография. Её можно приложить к удостоверению о работе.

photo-camera-no-target = Снимать можно только людей.
photo-camera-taken = Снимок готов.
photo-camera-taken-floor = Снимок готов, но фотография выпала на пол.
work-photo-name = фотография — {$name}

ent-PositionScanner = сканер должности
    .desc = Считывает удостоверение и заносит должность, тег и имя в реестр государства.

ent-WorkRegistryConsole = консоль управления должностями
    .desc = Реестр должностей и тегов государства. Через неё увольняют с должности: карта остаётся, но её тег больше не действует.

# --- удостоверение ---
work-card-issued-name = удостоверение: {$holder}
work-card-issued-desc = Пластиковая карта с должностью, подписями и фотографией владельца.
work-card-window-title = Удостоверение о работе
work-card-holder = Владелец:
work-card-position = Должность:
work-card-employer = Работодатель:
work-card-workplace = Место работы:
work-card-tag = Рабочий тег:
work-card-date = Дата:
work-card-signatures = Подписи:
work-card-signature = Подпись:
work-card-signatures-state = подписано от имени государства
work-card-photo = Фото
work-card-unknown = не указано
work-card-no-tag = нет
work-card-unsigned = не подписано
work-card-no-photo = нет фотографии
work-card-revoked = ТЕГ ОТОЗВАН
work-card-photo-attached = Фотография приложена к удостоверению.
work-card-no-photo-to-attach = Возьмите фотографию во вторую руку.
work-card-footer = Удостоверение оформляется по подписанному договору. Тег действует, пока его не отозвали из консоли должностей.
work-id-verb-open = Открыть удостоверение
work-id-examine-revoked = [color=#ff5555]Тег отозван государством.[/color]

# --- консоль управления должностями ---
work-registry-title = Консоль управления должностями
work-registry-verb-bind = Закрепить за государством
work-registry-bound = Закреплено за государством «{$name}».
work-registry-unbound = Сначала закрепите за государством.
work-registry-unbound-short = Не закреплена за государством
work-registry-faction = Государство: {$name}
work-registry-summary = Записей: {$count}, тегов: {$tags}
work-registry-empty = Реестр пуст. Отсканируйте удостоверение сканером должности.
work-registry-entry-details = тег: {$tag} · назначил: {$employer} · {$date}
work-registry-fire = Уволить
work-registry-restore = Восстановить
work-registry-fired = {$name} уволен(а) с должности: тег больше не действует.
work-registry-restored = {$name} восстановлен(а): тег снова действует.
work-registry-entry-missing = Запись не найдена.
work-registry-access-button = Настроить доступ
work-registry-access-title = Доступ к консоли
work-registry-access-use = Может пользоваться
work-registry-access-edit = Может редактировать
work-registry-access-no-tags = Тегов пока нет: отсканируйте первое удостоверение.
work-registry-access-footer = Пустой список слева пускает всех. Теги берутся из отсканированных удостоверений.
work-registry-access-saved = Списки доступа обновлены.
work-registry-no-power = Консоль обесточена.
work-registry-no-access = У вас нет доступа к консоли.
work-registry-save = Сохранить
work-registry-cancel = Отмена
work-registry-close = Закрыть
work-registry-footer = Увольнение не портит карту: тег просто перестаёт действовать на консолях и рабочих станциях.

position-scanner-unbound = Сначала закрепите сканер за государством.
position-scanner-no-card = Возьмите удостоверение во вторую руку.
position-scanner-empty = В удостоверении нет имени и должности.
position-scanner-scanned = {$name} — должность «{$position}» внесена в реестр.

uu-construction-step-paper = лист бумаги

ent-TypewriterKit = коробка с деталями печатной машинки
    .desc = Детали для сборки печатной машинки. Вскройте коробку, чтобы получить материалы.

kit-box-unpacked = Вы вскрыли коробку и вытряхнули содержимое.
