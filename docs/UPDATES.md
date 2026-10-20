# Обновления и поставка

Модуль `RevEx.Desktop.Updates` включён в main при объединении ветки `realese`.
Авторизация перенесена из отдельного проекта Auth в `RevEx.Desktop.Core/Authentication`.
API-фасад Core теперь предоставляет предметные репозитории; общий HTTP-код
находится в `RevExApiTransport`.

## Запуск установленного приложения

1. `Program.Main` вызывает `DesktopUpdates.Initialize()` до блокировки экземпляра и Avalonia.
2. `StartupUpdateCoordinator` проверяет наличие управляемой установки Velopack.
3. Для установленного клиента backend определяет `current`, `optional` или `required`
   и конкретный пакет платформы. Модуль не выбирает релиз самостоятельно.
4. Стартовое окно показывает решение и позволяет сменить адрес backend.
5. При загрузке проверяются размер и SHA256, применяется выданное предложение.
6. После перезапуска и OIDC-входа `ReportAuthenticatedAsync` подтверждает версию.
   Ошибка отправки подтверждения записывается в stderr и не блокирует главное окно.

Идентификатор, ключ установки и незавершённое подтверждение находятся в
`desktop-installation.json` рядом с пользовательскими настройками, вне каталога приложения.
Ключ не передаётся в URL. При `dotnet run` без установки сценарий обновления пропускается.
Отсутствие активного серверного релиза может блокировать установленный клиент до входа.

## Сборка установщиков

Workflow `.github/workflows/windows-installer.yml` запускается при push в **realese**.
Сам merge в main не запускает этот workflow. Имя ветки сохранено в исходном написании.
Windows x64, macOS Intel и Apple Silicon собираются независимо; macOS-архив объединяет
обе архитектуры. Версия определяется workflow, упаковка использует Velopack 1.2.161.

См. [порядок выпуска](../packaging/RELEASING.md),
[Windows](../packaging/windows/README.md), [macOS](../packaging/macos/README.md)
и [описание модуля](../RevEx.Desktop.Updates/README.md).
ZIP-комплект для загрузки в Web отличается от внешнего ZIP GitHub Artifacts.

## Проверки

```powershell
dotnet build RevEx.Desktop.sln -c Release --disable-build-servers -m:1
dotnet run --project tests/StartupUpdateChecks/StartupUpdateChecks.csproj -c Release --disable-build-servers
```

Исполняемые проверки покрывают стартовые решения, идентичность установки,
ограничения источника и проверку пакета. Они не заменяют реальную установку,
обновление A → B и откат на каждой платформе. Публикация релиза и установка
пакетов при выполнении merge не производились.
