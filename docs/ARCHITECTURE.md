# Архитектура Desktop

| Проект | Назначение |
| --- | --- |
| `RevEx.Desktop` | Avalonia 11.3.2, MVVM CommunityToolkit, окна, вкладки, DI, конфигурация. |
| `RevEx.Desktop.Core` | DTO каталога, интерфейсы, фасад RevExApiService, предметные репозитории, RevExApiTransport, mock API, роли и Authentication. |
| `RevEx.Desktop.Updates` | Стартовая проверка политики backend, окно обновления, пакеты и установка через Velopack. |
| `RevEx.Configuration` | Пути настроек и загрузок, хранение user-settings.json, адрес регистрации. |
| `RevEx.Connector.Contracts` | DTO и интерфейс обмена с коннекторами. |
| `RevEx.Connector.Host` | HTTP-хост коннектора и регистрация в Desktop. |
| `RevEx.Desktop.Connectors` | Локальный сервер Desktop, реестр коннекторов и HTTP-клиент. |
| `RevEx.Connector.Mock` | Исполняемый пример коннектора. |
| `RevEx.LoadingQueue` | Элементы очереди и смена статусов загрузки. |

Все текущие проекты нацелены на net8.0. Desktop.Core сейчас не предоставляет
netstandard2.0; это существенно для совместимости с плагином Revit 2020.

## Последовательность запуска

`Program.cs` запускает Avalonia. `App.axaml.cs` читает конфигурацию,
регистрирует зависимости через `DI/ServiceExtensions.cs`, затем:

1. Выполняет StartupUpdateCoordinator: установленный клиент проверяет обновления до входа; IDE-сборка пропускает проверку. Затем запускает DesktopConnectorServer на loopback-порту.
2. Получает access token через IAuthenticationService; при необходимости открывает браузер.
3. После успешного callback отправляет подтверждение версии через ReportAuthenticatedAsync и показывает MainWindow. Ошибка подтверждения не блокирует работу.
4. При исключении показывает AuthenticationErrorWindow вместо главного окна.

Поэтому отсутствие главного окна во время входа — часть текущего сценария запуска.
Токены OIDC хранятся в памяти сервиса; получение access token защищено блокировкой,
при истечении выполняется refresh либо новый вход. Не добавляйте токены в конфиги/Git.

## UI и данные

`Views` содержит Avalonia-разметку, `ViewModels` — состояние и команды.
`Navigation/WorkspaceTabFactory` создаёт вкладки; `MainMenuProvider` определяет меню.
Фасад `RevExApiService` предоставляет Contents, Categories, Tags, TagGroups, ContentStatuses, ContentVersions, ContentFiles и ContentHistory. Предметные HTTP-репозитории используют `RevExApiTransport`; DI задаёт базовый URL и добавляет bearer-токен
через AuthenticationHandler. MockRevExApiService существует, но штатная DI использует HTTP-клиент.

Каталог показывает карточки и фильтры; детали включают версии, файлы и историю.
Редактор, категории, теги, настройки и очередь загрузок представлены отдельными вкладками.
Сервер остаётся источником истины по правам и валидации.

| Роль | Пункты меню |
| --- | --- |
| user | Каталог, настройки. |
| moderator | Дополнительно добавление контента и загрузки. |
| administrator | Дополнительно категории и группы тегов. |

При выходе отзывается refresh token, затем приложение завершается.
Режим ActiveDirectory предусмотрен отдельной веткой конфигурации;
WindowsAuthenticationService не выдаёт OIDC-токен и содержит пустой набор ролей.
Работа локального стенда проверялась только с Keycloak.


Реализация Connector.Host, Connector.Mock, общие Contracts и Configuration вынесены в [RevEx.Connecter](https://github.com/DenisFazlul/RevEx.Connecter). Перед сборкой клонируйте его рядом с RevEx.Desktop: проекты используют относительные ProjectReference. Сервер регистрации Desktop и HTTP-клиент остаются здесь.
