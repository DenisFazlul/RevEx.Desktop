# RevEx.Desktop.Updates

Отдельная сборка обновления десктопа. Backend возвращает решение `current`,
`optional` или `required` и назначает конкретный пакет для платформы клиента.
Модуль не выбирает релизы и не сравнивает диапазоны поддержки.

В составе: контракты ответа, запрос решения, локальная идентичность установки,
окно и его модель, координатор запуска, проверка SHA256 и размера,
загрузка через закреплённое предложение, установка и откат через Velopack,
подтверждение фактически запущенной версии после перезапуска и входа.

Приложение вызывает `DesktopUpdates.Initialize()` до Avalonia и блокировки
единственного экземпляра, регистрирует `AddDesktopUpdates()` и вызывает
`StartupUpdateCoordinator.RunAsync()` до авторизации. После входа вызывает
`DesktopUpdateService.ReportAuthenticatedAsync()` с проверенным access token.

`DesktopUpdates.InstalledVersion` читает локальный VelopackLocator без URL.
Идентификатор, случайный ключ и ожидающие подтверждения переходы сохраняются
в `desktop-installation.json` рядом с пользовательскими настройками,
вне заменяемого каталога приложения. Ключ передаётся в теле проверки или
заголовке скачивания, не в URL. На Unix файл доступен только владельцу.

Сборка зависит от контрактов Core и Configuration, но не от исполняемого проекта.
Стили окна наследуются от приложения. IDE-запуск без установки пропускает обновления.

Проверки: `dotnet build tests/StartupUpdateChecks/StartupUpdateChecks.csproj --disable-build-servers -m:1`,
затем `dotnet tests/StartupUpdateChecks/bin/Debug/net8.0/StartupUpdateChecks.dll`.
Реальную установку, откат и перезапуск проверяют на приложении, установленном Velopack.
