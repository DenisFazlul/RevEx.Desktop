# Установщики macOS

После merge или push в `realese` workflow `Desktop installers` собирает вместе:

- Windows x64: установочный `.exe`.
- macOS Intel: `RevEx-Setup-1.0.N-osx-x64.pkg`.
- macOS Apple Silicon: `RevEx-Setup-1.0.N-osx-arm64.pkg`.

Все пакеты одного запуска имеют общую версию `1.0.N`.

GitHub → Actions → Desktop installers → успешный запуск → Artifacts.
Выберите артефакт `RevEx-macOS-Installer-osx-x64-1.0.N` для Intel или
`RevEx-macOS-Installer-osx-arm64-1.0.N` для Apple Silicon. Распакуйте ZIP,
запустите `.pkg` и пройдите шаги установщика. Приложение появится в
`/Applications/RevEx.app`. Установка туда требует прав администратора.
Runtime .NET включён в пакет. Автообновления в приложение не добавляются.

CI проверяет архитектуру исполняемого файла, структуру и подпись `.app`, реальную
установку `.pkg`, установленную версию и наличие конфигурации и runtime.
Рабочее пространство не запускается в CI, поскольку требует backend и авторизации.

Пока используется только ad-hoc подпись приложения. Она не подтверждает издателя:
Developer ID и Apple notarization не настроены, `.pkg` не подписан сертификатом.
Gatekeeper может блокировать установку или запуск. Для публичного распространения
потребуются сертификаты Apple и отдельная настройка секретов GitHub Actions.

Удаление: переместите `/Applications/RevEx.app` в Корзину. Пользовательские настройки
в `~/Library/Application Support/RevEx` при этом сохраняются.
Отдельные приложения коннекторов в пакет не включены. Artifacts хранятся 90 дней;
GitHub Releases автоматически не создаются.

## Локальная сборка на Mac

Из корня репозитория, для Apple Silicon (для Intel замените RID на `osx-x64`):

```bash
dotnet publish RevEx.Desktop/RevEx.Desktop.csproj -c Release -r osx-arm64 --self-contained true -p:Version=1.0.0 -o artifacts/publish
bash packaging/macos/build-installer.sh 1.0.0 osx-arm64
```

Скрипт требует отсутствия `artifacts/macos/RevEx.app`, чтобы не смешивать разные
сборки. Для повторной упаковки используйте чистую рабочую копию или предварительно
уберите только ранее сгенерированный bundle.
