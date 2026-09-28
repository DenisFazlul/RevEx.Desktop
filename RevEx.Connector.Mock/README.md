# RevEx Connector Mock

Локальный HTTP-сервер, имитирующий загрузку файлов версии в активный проект:

- `POST /api/v1/projects/active/content-versions/load`

Запуск из корня desktop solution:

```bash
dotnet run --project RevEx.Connector.Mock/RevEx.Connector.Mock.csproj
```

Сервер выбирает свободный loopback-порт и регистрируется в Desktop. Для каждого
полученного файла он сообщает статусы `Accepted`, `Started` и `Completed` через HTTP
callback, выдерживая между статусами паузу 13 секунд.
