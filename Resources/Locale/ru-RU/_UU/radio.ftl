## Radio — Названия предметов
## Имена и описания сущностей локализуются движком через ключ ent-<ProtoId>
## (см. Robust.Shared/Localization/LocalizationManager.Entity.cs), поэтому
## строки вида radio-*-name в YAML работать не будут.
ent-RadioTransmitter = Передатчик
    .desc = Создаёт и передаёт на радиочастотах. Можно настроить только один раз.

ent-RadioDevice = Рация
    .desc = Принимает радиопередачи. Привяжите к передатчику для получения сообщений.

ent-RadioEarpiece = Гарнитура
    .desc = Незаметная гарнитура для приёма радиосообщений. Привяжите к передатчику.

ent-FrequencyConfigurator = Конфигуратор частот
    .desc = Копирует частоты с передатчиков. Используйте на передатчике для копирования, затем на рации или гарнитуре для передачи.

ent-SignalTower = Сигнальная вышка
    .desc = Увеличивает дальность радио на 20 клеток.

ent-GeneralJammer = Глушитель
    .desc = Глушит все радиопередачи в радиусе действия.

ent-SpecificJammer = Глушитель частоты
    .desc = Глушит определённую радиочастоту в радиусе действия.


## Radio — Интерфейс
radio-transmitter-title = Передатчик
radio-no-frequency = Частота не задана
radio-enter-frequency = Введите частоту (1-3 символа):
radio-frequency-placeholder = напр. АБВ
radio-set-frequency = Задать
radio-current-frequency = Текущая частота: {$freq}
radio-invalid-frequency = Некорректная частота! Используйте 1-3 символа.
radio-set-success = Успешно!
radio-jammed = Передача заглушена!
radio-transmit-on = Передача включена!
radio-transmit-off = Передача выключена!
radio-enter-name = Имя частоты (необязательно):
radio-name-placeholder = напр. Командный
radio-bound = Привязано к частоте «{$frequency}»!
radio-already-bound = Уже привязано к частоте «{$frequency}».
radio-already-configured = Передатчик уже настроен! Разберите его чтобы перенастроить.
radio-configurator-copied = Частота «{$frequency}» скопирована!
radio-configurator-pasted = Частота «{$frequency}» передана!
radio-configurator-empty = Конфигуратор пуст! Скопируйте частоту с передатчика.
radio-jammer-no-target = Целевая частота не задана. Настройте глушитель конфигуратором частот.
radio-jammer-target = Глушит частоту: {$frequency}
radio-jammer-frequency-set = Глушитель нацелен на частоту «{$frequency}».

radio-transmitter-footer = Одноразовая настройка. Для смены частоты разберите передатчик.


## Сигнальная вышка
tower-window-title = Сигнальная вышка
tower-enter-code = Код доступа:
tower-set-code = Код доступа (задайте при первой настройке):
tower-code-placeholder = напр. AXIS-141
tower-apply = Задать
tower-footer = Код знает только узкий круг. Конфигуратор запертую вышку не откроет.
tower-invalid-code = Код — от 3 до 12 букв или цифр.
tower-code-set = Код задан. Запомните его — сменить сможет только тот, кто знает код.
tower-wrong-code = Неверный код доступа.
tower-frequency-set = Вышка настроена на частоту «{$freq}».
tower-locked = Вышка заперта кодом — конфигуратор не поможет.
tower-status-locked = Заперта кодом
tower-status-open = Код не задан