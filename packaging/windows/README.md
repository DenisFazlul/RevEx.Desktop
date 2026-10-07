# Установщик Windows

> Ниже сохранена инструкция прежнего ручного процесса Inno Setup. Текущий CI
> использует Velopack: `RevEx.Desktop-win-x64-Setup.exe`, артефакт
> `desktop-win-x64-1.0.N`. Для публикации обновлений используйте общий архив
> `RevEx-Desktop-Release-1.0.N`. Актуальная инструкция: [RELEASING.md](../RELEASING.md).
> Старую установку необходимо один раз заменить установкой Velopack.

## Архивная инструкция (не текущий CI)

Каждый push в ветку `realese`, включая merge pull request, запускает workflow
`Desktop installers`. Результат — установочный `RevEx-Setup-1.0.N-win-x64.exe`,
где N — номер запуска workflow. Автообновления в приложение не добавляются.

## Как получить установщик

1. Создайте pull request с целевой веткой `realese` и выполните merge.
2. Откройте GitHub → Actions → Desktop installers → соответствующий запуск.
3. После успешной сборки скачайте `RevEx-Windows-Installer-1.0.N` из Artifacts.
4. Распакуйте скачанный ZIP: внутри находится установочный `.exe`.

GitHub Releases этим workflow не создаются. Артефакт хранится 90 дней.

Установщик включает .NET runtime: отдельная установка .NET на компьютере пользователя
не нужна. Приложение устанавливается для текущего пользователя в
`%LOCALAPPDATA%\Programs\RevEx`, создаёт ярлык в меню «Пуск» и запись удаления.
Ярлык на рабочем столе можно выбрать при установке. Повторная установка обновляет
ту же копию приложения. Пользовательские настройки вне папки установки сохраняются.
В пакет входит десктоп и его библиотеки; отдельные приложения коннекторов не включены.

После упаковки CI проверяет тихую установку, наличие приложения, конфигурации и runtime,
а затем удаление приложения. Запуск рабочего пространства требует доступного backend
и авторизации и не проверяется этим workflow.

Установщик пока не подписан сертификатом; Windows может показывать предупреждение
SmartScreen. Установщики macOS собираются отдельными заданиями того же workflow;
инструкция находится в `packaging/macos/README.md`.

## Локальная сборка на Windows

Требуются .NET SDK 8 и Inno Setup 6.

```powershell
dotnet publish RevEx.Desktop/RevEx.Desktop.csproj -c Release -r win-x64 --self-contained true -p:Version=1.0.0 -o artifacts/publish
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0 packaging/windows/RevEx.Desktop.iss
```
