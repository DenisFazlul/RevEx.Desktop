# Разработка и ограничения

## Сборка

```powershell
dotnet restore RevEx.Desktop.sln
dotnet build RevEx.Desktop.sln -c Release --no-restore
```

Release-сборка проверена без ошибок и предупреждений 20.10.2026.
Проверки модуля обновления находятся в `tests/StartupUpdateChecks` и запускаются через `dotnet run --project tests/StartupUpdateChecks/StartupUpdateChecks.csproj -c Release`. Не подменяйте smoke-проверку
приложения только успешной компиляцией: OIDC и каталог требуют запущенного сервера.

Изменение UI проверяйте запуском соответствующей вкладки; изменение API DTO —
сверкой с `RevEx.Web/docs/openapi.json`, сборкой клиента и реальным запросом.
Изменение коннекторов — запуском Mock и проверкой регистрации/callback.
Не изменяйте вручную сборочные каталоги.

## Публикация

Для локальной framework-dependent публикации Windows:

```powershell
dotnet publish RevEx.Desktop/RevEx.Desktop.csproj -c Release -r win-x64 --self-contained false -o artifacts/win-x64
```

Это папка публикации, а не установщик и не ZIP-контракт релизного сервера.
Упаковку, поддерживаемые платформы, публикацию и политику версий смотрите в
`RevEx.Web/docs/DESKTOP_RELEASES.md`. Текущий Program.cs вызывает DesktopUpdates.Initialize(); установленный клиент выполняет сценарий обновления по политике backend. См. [модуль обновления](UPDATES.md).

## Совместимость Revit 2020

RevitPlugin ссылается на соседний Desktop.Core, но сам использует .NET Framework 4.7.2.
Текущий Core — net8.0; прямая такая ссылка несовместима. Прежний README плагина
описывал локальную адаптацию netstandard2.0, которая отсутствует в свежем Desktop.
Возможные отдельные работы: выделение совместимого контракта либо осознанная
адаптация Core. В рамках документации исходники и target frameworks не менялись.

## Проверенный результат и пределы

API-порт 8082 согласован в исходном appsettings с Compose Web.
Локальный OIDC admin, главное окно и каталог проверены.
Реальная загрузка в Revit, ActiveDirectory и установщики всех платформ в этой сессии
не проверялись; не считайте их подтверждёнными по одной сборке Desktop.

## Макеты интерфейса

Папка design содержит SVG-макеты catalog-filter-window.svg, revex-content-details.svg и revex-desktop-ui.svg. Это визуальные ориентиры; фактический интерфейс определяется Avalonia Views и ViewModels.
