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


Клиент коннектора, его интерфейс и очередь задач находятся в соседнем репозитории
`RevEx.Connector.Client`. Приёмник команд и общий TCP-протокол находятся в
[RevEx.Connecter](https://github.com/DenisFazlul/RevEx.Connecter).
Для сборки расположите `RevEx.Desktop`, `RevEx.Connector.Client`, `RevEx.Connecter`
и `RevEx.Domain` рядом. Desktop подключает клиент через ProjectReference;
внешние проекты клиента, очереди, контрактов и протокола добавлены в RevEx.Desktop.sln.


Общие сущности и DTO вынесены в отдельный репозиторий: [подключение Domain](docs/DOMAIN.md).
