# Локальные коннекторы

Desktop управляет каталогом через серверный API и отдельно взаимодействует
с приложениями на этой машине через loopback HTTP. Эти протоколы не заменяют друг друга.

## Маршруты Desktop

По умолчанию `http://127.0.0.1:5060`:

| Метод и путь | Назначение |
| --- | --- |
| `POST /api/v1/connectors/register` | Зарегистрировать экземпляр приложения. |
| `DELETE /api/v1/connectors/{instanceId}` | Удалить запись из локального реестра. |
| `POST /api/v1/loading/status` | Обновить статус файла в очереди. |

Регистрация передаёт InstanceId, Name, Product, ProductVersion, BaseAddress,
ProcessId, ActiveDocument. Product определён enum Revit/AutoCad/Mock.
BaseAddress должен быть абсолютным loopback HTTP URL.
Проверка регистрации не доказывает, что сторонний процесс действительно реализует все команды.

## Маршруты Connector.Host

Коннектор слушает выделенный свободный loopback-порт:

| Метод и путь | Назначение |
| --- | --- |
| `GET /api/v1/connector/health` | Состояние, InstanceId и активный документ. |
| `POST /api/v1/projects/active/content-versions/load` | Загрузка файлов версии в активный проект. |

`LoadContentVersionRequest`: массив Files (Id, Name, Path) и StatusCallbackAddress.
Результат: Success и LoadedFileCount. Callback передаёт Id и Status:
Accepted → Started → Completed. При неизвестном ID Desktop возвращает 404,
при недопустимом статусе — 400. Контракты находятся в `RevEx.Connector.Contracts`.
Путь файла локален для этой машины; не следует выдавать его за серверный URL.

## Mock-сценарий

Сначала запустите Desktop, затем из корня solution:

```powershell
dotnet run --project ../RevEx.Connecter/RevEx.Connector.Mock/RevEx.Connector.Mock.csproj
```

Mock выбирает порт и регистрируется в Desktop, имитирует обработку каждого файла
и отправляет статусы с паузами 13 секунд. Он не загружает реальные семейства в Revit.
Адрес регистрации берётся из общего пользовательского файла настроек;
проверьте его совпадение с адресом Desktop.

Для ручной проверки откройте «Настройки» Desktop, обновите список коннекторов,
проверьте health и затем очередь загрузок при отправке файлов.
Удаление записи из реестра не завершает процесс коннектора.

## Граница с RevitPlugin

Текущий соседний RevitPlugin — WPF-плагин Revit 2020 с ExternalEvent и информацией
о документе. В его исходниках нет регистрации Connector.Host и HTTP-загрузки файлов.
Наличие ProjectReference на Desktop.Core не означает реализацию этого протокола.


Реализация Connector.Host, Connector.Mock, общие Contracts и Configuration вынесены в [RevEx.Connecter](https://github.com/DenisFazlul/RevEx.Connecter). Перед сборкой клонируйте его рядом с RevEx.Desktop: проекты используют относительные ProjectReference. Сервер регистрации Desktop и HTTP-клиент остаются здесь.
