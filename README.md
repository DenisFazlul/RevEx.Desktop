# RevEx Desktop

Клиент библиотеки контента RevEx на Avalonia и .NET 8: каталог, версии файлов,
редактирование, справочники, история и локальные коннекторы.

Полная [документация](docs/README.md) находится в корневой папке `docs`.
Docker Compose в этом репозитории отсутствует: сервер запускается из RevEx.Web.

```powershell
dotnet build RevEx.Desktop.sln -c Release
dotnet run --project RevEx.Desktop/RevEx.Desktop.csproj -c Release
```

Перед запуском поднимите Web/API и Keycloak. Текущий API — `http://localhost:8082/`,
Keycloak — `http://localhost:8081/realms/revex`.
См. [настройку и запуск](docs/OPERATIONS.md) и [контракт коннекторов](docs/CONNECTORS.md).


Реализация Connector.Host, Connector.Mock, общие Contracts и Configuration вынесены в [RevEx.Connecter](https://github.com/DenisFazlul/RevEx.Connecter). Перед сборкой клонируйте его рядом с RevEx.Desktop: проекты используют относительные ProjectReference. Сервер регистрации Desktop и HTTP-клиент остаются здесь.


Общие сущности и DTO вынесены в отдельный репозиторий: [подключение Domain](docs/DOMAIN.md).
