# Локальная связь Desktop и коннекторов

Общий код находится в RevEx.RevitPlugin/RevEx.RevitPlugin.Connector (net472 и
netstandard2.0). Коннектор не зависит от Autodesk API. TCP и регистрация в каталоге
находятся в Implementations/Tcp и Implementations/LocalDirectory.

## Задача

Desktop создаёт `ConnectorTask` в Domain: `Id`, `Name`, `Command`.
Любая прикладная команда отправляется вместе с этой сущностью и своим payload.
Идентификатор задачи является также идентификатором запроса/ответа TCP.
Файлы являются данными команды; их идентификаторы не используются коннектором
для определения задачи или отправки её статусов.

У задачи ровно три состояния `ConnectorTaskStatus`:

1. `Sent = 0` — Отправлено (Desktop начал отправку).
2. `Accepted = 1` — Принято (коннектор получил задачу).
3. `Done = 2` — Готово (выполнение закончено).

`ConnectorTaskUpdate` содержит Id, Status, Message и Sequence. Message — произвольная
информационная строка, например «Открываю документ»; она не определяет состояние.
Sequence позволяет игнорировать устаревшие уведомления и повторные снимки состояния.
При ошибке задача также переходит в Done: пояснение находится в Message,
а ошибка команды возвращается отдельно в ответе. Done означает окончание обработки.

Desktop хранит задачи в общем `IConnectorTaskQueue` / `ConnectorTaskQueue`.
В интерфейсе состояние и информационная строка отображаются отдельно.

## Исполнитель

Коннектор выбирает `IConnectorCommandExecutor` по enum `ConnectorCommand` из Domain.
Внешний обработчик наследует типизированную базу:

```csharp
public sealed class LoadExecutor
    : ConnectorCommandExecutor<ConnectorLoadRequest, LoadContentVersionResponse>
{
    public override ConnectorCommand Command => ConnectorCommand.LoadContentVersion;

    protected override async Task<LoadContentVersionResponse> ExecuteAsync(
        ConnectorLoadRequest request, IConnectorCommandContext context,
        CancellationToken token)
    {
        await context.ReportStatusAsync("Открываю документ", token);
        // Execute work in the host application's valid API context.
        return new LoadContentVersionResponse(true, request.Files.Count);
    }
}

connector.RegisterExecutor(new LoadExecutor());
```

Контекст содержит `Task` и метод обновления информационной строки. Исполнитель не
получает TCP-соединение, не передаёт FileLoadingStatusUpdate и не переключает состояния:
Accepted и Done публикуются общим сервисом автоматически. Для Revit работа выполняется
через ExternalEvent; реальная загрузка пока остаётся заглушкой в плагине.

Исполнители по умолчанию требуют исключительного выполнения. Следующая такая задача
при занятости получает ответ с ошибкой. Служебные health/statuses не блокируются загрузкой
и при обычном служебном запросе не создают пользовательскую историю. Повторная регистрация
команды отклоняется. Новые команды добавляются в enum Domain.

`IConnectorClient.ExecuteAsync<TRequest, TResponse>` отправляет произвольную прикладную
задачу. `LoadContentVersionAsync` — обёртка: создаёт по задаче на файл и вызывает тот же
общий метод, без собственного механизма статусов.

## Разрыв связи и повтор

Токен выполнения связан с жизнью коннектора, а не TCP-соединением. Отключение Desktop
не прерывает принятую работу. Статусы остаются в памяти и запрашиваются при подключении.
Повторный запрос с тем же Id и теми же данными получает сохранённый результат;
исполнитель повторно не запускается. Использование Id с другими данными запрещено.
Состояние и результаты пока не сохраняются после завершения процесса коннектора.
Desktop обновляет известные ему задачи; очередь после полного перезапуска Desktop
не восстанавливается с диска.

## Обнаружение

Коннектор выбирает свободный loopback TCP-порт (или ConnectorHostOptions.Port),
после запуска атомарно публикует `<InstanceId>.json`. Desktop читает каталог,
проверяет health и идентификатор экземпляра, следит за FileSystemWatcher и дополнительно
сверяет состояние раз в 5 секунд. Недоступные и некорректные записи не показываются.
Коннектор может работать без Desktop неограниченно долго, без попыток подключения к нему.
При штатной остановке удаляется только собственная регистрация.

Каталоги:

- Windows: `%LOCALAPPDATA%/RevEx/connectors`.
- macOS: `~/Library/Application Support/RevEx/connectors`.
- Linux: `$XDG_CONFIG_HOME/RevEx/connectors` или `~/.config/RevEx/connectors`.

## Протокол версии 2

Регистрация содержит ProtocolVersion = 2 и объект Connector (ConnectorDescriptor).
TCP-кадр: 4 байта длины big-endian, затем UTF-8 JSON до 8 MiB.
Envelope: Version = 2, Kind, Id, Name, TaskName, Payload, Error.
Name содержит wire-имя команды либо имя события; TaskName — название задачи.
Id запроса — GUID задачи. Ответ содержит тот же Id. Kind = task означает прикладную
задачу с жизненным циклом; Kind = request — служебный запрос без записи в историю.

Команды: `Health = 1` (health), `GetTaskStatuses = 2` (statuses),
`LoadContentVersion = 3` (load). Информационные уведомления идут событием `taskStatus`
с ConnectorTaskUpdate. Payload загрузки содержит Files с локальными путями.
HTTP callback в активной схеме отсутствует. Предыдущий HTTP-код сохранён отдельно
в Implementations/Http; его DTO остаются для совместимости архивной реализации.

## TestHub и проверки

TestHub создаёт один коннектор и регистрирует один MockLoadExecutor без аргументов.
Он ожидает команды от Desktop и завершается по Enter. Mock имитирует загрузку и
сообщает текст через контекст; Revit не требуется.

Из корня RevEx.RevitPlugin:

```powershell
dotnet build tests/DomainIntegrationHost/DomainIntegrationHost.csproj
dotnet run --project ../RevEx.Desktop/tests/DomainIntegrationChecks/DomainIntegrationChecks.csproj -- tests/DomainIntegrationHost/bin/Debug/net472/DomainIntegrationHost.exe
```

Проверяются три состояния, произвольные сообщения, независимость Id задачи от файлов,
повтор без повторного выполнения, ошибки, разрыв/восстановление связи и обмен
между .NET Framework 4.7.2 и .NET 8. Проверки выполнены на Windows; macOS требует прогона.
