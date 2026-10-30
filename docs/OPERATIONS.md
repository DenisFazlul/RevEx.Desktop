# Запуск и настройки

## Требования

Для сборки — .NET SDK с поддержкой net8.0, для запуска framework-dependent сборки —
runtime .NET 8. На этой машине сборка проверена SDK 10.0.401, установлен runtime .NET 8.
Нужны рабочий браузер, API и Keycloak из соседнего RevEx.Web.

```powershell
dotnet build RevEx.Desktop.sln -c Release
dotnet run --project RevEx.Desktop/RevEx.Desktop.csproj -c Release
```

Готовый exe Windows находится в `RevEx.Desktop/bin/Release/net8.0/RevEx.Desktop.exe`.
При пересборке закройте запущенный экземпляр, чтобы не блокировать DLL.

## Настройки

Штатный файл — `RevEx.Desktop/appsettings.json`, копируется при сборке.
Сверху загружается необязательный пользовательский JSON.

| Ключ под AppSettings | Текущее значение / назначение |
| --- | --- |
| `ApiPath` | `http://localhost:8082/` — опубликованный API Web. |
| `ConnectorRegistrationAddress` | Устаревшая настройка HTTP-реализации; TCP-обнаружение её не использует. |
| `Authentication:Provider` | `Keycloak`. |
| `Authentication:RedirectUri` | `http://127.0.0.1:7890/` — OIDC callback. |
| `Authentication:Providers:Keycloak:Authority` | `http://localhost:8081/realms/revex`. |
| `Authentication:Providers:Keycloak:ClientId` | `revex-desktop`. |
| `Authentication:Providers:Keycloak:Scope` | `openid profile`. |
| `Authentication:Providers:Keycloak:RequireHttpsMetadata` | `false` для локального HTTP-стенда. |

API-адрес должен быть абсолютным HTTP(S), адрес регистрации — loopback HTTP,
callback — с хостом 127.0.0.1 и разрешённым redirect в Keycloak.

| ОС | Пользовательский файл |
| --- | --- |
| Windows | `%LOCALAPPDATA%/RevEx/user-settings.json` |
| macOS | `~/Library/Application Support/RevEx/user-settings.json` |
| Linux | `$XDG_CONFIG_HOME/RevEx/user-settings.json`, fallback `~/.config/RevEx/user-settings.json` |

В этом же каталоге есть `Downloads`. Экран «Настройки» сохраняет ApiPath и
ConnectorRegistrationAddress внутри объекта `AppSettings` атомарной заменой файла.
Сохранённые значения применяются после перезапуска Desktop и коннекторов.
Пользовательский файл имеет приоритет: изменение исходного appsettings не отменяет его значений.

## Вход и проверка

На локальном стенде настроены `user/user` и `admin/admin`; пароль moder хранится
в исключённом из Git `RevEx.Web/.env`. Приложение использует realm revex,
а консоль управления Keycloak — отдельный realm master.

1. Убедитесь, что API 8082 и Keycloak 8081 готовы.
2. Запустите Desktop и войдите через браузер.
3. Дождитесь главного окна RevEx и карточек каталога.
4. Для admin проверьте наличие категорий/групп тегов; для user — меню чтения.

20.10.2026 проверен вход admin, главное окно и загрузка карточек каталога.
Для user и admin отдельно проверен OIDC и чтение категорий API с HTTP 200.

## Диагностика

| Симптом | Что проверить |
| --- | --- |
| Нет главного окна | Завершён ли браузерный вход; startup ждёт токен. |
| AuthenticationErrorWindow | Текст ошибки, Keycloak metadata, свободные 5060/7890, настройки callback. |
| Неверный логин/пароль | Используйте revex, а не master; фактический пароль хранится в Keycloak. |
| 404 от Caddy | Старый ApiPath 8080; нужен 8082, включая user-settings.json. |
| 401/403 | Срок/issuer/audience токена и роли пользователя. |
| MSB3026 при сборке | Запущенный процесс удерживает DLL; закройте Desktop. |
| Коннектор не виден | Он запущен после Desktop и зарегистрирован по тому же адресу. |
| Настройки не применились | Нужен перезапуск; проверьте пользовательский JSON и его приоритет. |

## Установленный клиент и обновления

После объединения realese в main установленный через Velopack клиент проверяет политику backend до авторизации. Без активного серверного релиза рабочее пространство может не открыться. Обычная сборка dotnet run пропускает эту проверку. Состояние установки хранится в desktop-installation.json рядом с пользовательскими настройками. См. [обновления](UPDATES.md). Проверка главного окна от 20.10.2026 выше относится к сборке до этого merge.
