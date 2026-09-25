# RevEx Connector Mock

Небольшой локальный HTTP-сервер, имитирующий два метода Revit Connector:

- `POST /api/v1/families/inspect`
- `POST /api/v1/projects/active/families/load`

Запуск из корня desktop solution:

```bash
dotnet run --project RevEx.Connector.Mock/RevEx.Connector.Mock.csproj
```

По умолчанию сервер слушает только `http://127.0.0.1:5055`. Настройка совпадает с
`AppSettings:ConnectorConnection:BaseAddress` desktop-приложения.

Mock проверяет только наличие пути и расширение `.rfa`; настоящий файл не требуется.
Название семейства формируется из имени файла, категория — из нескольких известных
фрагментов имени (`door`, `window`, `chair`), иначе возвращается `Generic Models`.
