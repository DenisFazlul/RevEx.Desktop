using RevEx.Connector.Contracts;
using RevEx.Connector.Host;
using System.Net.Http.Json;

await using var connector = new ConnectorHost(new ConnectorHostOptions(
    "Connector Mock",
    ConnectorProduct.Mock,
    "1.0",
    ConnectorHostOptions.DefaultDesktopRegistrationAddress,
    "Mock Project.rvt"));
using var statusClient = new HttpClient();

connector.ContentVersionLoadRequested += async (request, cancellationToken) =>
{
    if (request.Files.Count == 0)
        throw new ArgumentException("Список файлов версии пуст.");

    foreach (var file in request.Files)
    {
        if (file.Id == Guid.Empty || string.IsNullOrWhiteSpace(file.Name))
            throw new ArgumentException("Файл версии не содержит идентификатор или имя.");
        if (string.IsNullOrWhiteSpace(file.Path) || !File.Exists(file.Path))
            throw new FileNotFoundException("Файл версии не найден.", file.Path);

        await SendStatusAsync(
            statusClient,
            request.StatusCallbackAddress,
            file.Id,
            FileLoadingStatus.Accepted,
            cancellationToken);
    }

    Console.WriteLine($"Mock принял загрузку: {request.Files.Count} файл(ов)");
    _ = SimulateLoadingAsync(statusClient, request);

    return new LoadContentVersionResponse(true, request.Files.Count);
};

connector.PingReceived += (_, _) =>
    Console.WriteLine($"Получен ping от Desktop: {DateTimeOffset.Now:O}");

await connector.StartAsync();
Console.WriteLine($"Connector Mock запущен: {connector.BaseAddress}");

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

try
{
    await Task.Delay(Timeout.Infinite, shutdown.Token);
}
catch (OperationCanceledException)
{
    // Штатное завершение: await using вызовет unregister и остановит HTTP Host.
}

static async Task SimulateLoadingAsync(HttpClient client, LoadContentVersionRequest request)
{
    try
    {
        foreach (var file in request.Files)
        {
            await Task.Delay(TimeSpan.FromSeconds(13));
            await SendStatusAsync(
                client,
                request.StatusCallbackAddress,
                file.Id,
                FileLoadingStatus.Started,
                CancellationToken.None);
            Console.WriteLine($"Начата загрузка: {file.Name}");

            await Task.Delay(TimeSpan.FromSeconds(13));
            await SendStatusAsync(
                client,
                request.StatusCallbackAddress,
                file.Id,
                FileLoadingStatus.Completed,
                CancellationToken.None);
            Console.WriteLine($"Загрузка завершена: {file.Name}");
        }
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Не удалось передать статус загрузки: {exception.Message}");
    }
}

static async Task SendStatusAsync(
    HttpClient client,
    Uri callbackAddress,
    Guid id,
    FileLoadingStatus status,
    CancellationToken cancellationToken)
{
    using var response = await client.PostAsJsonAsync(
        callbackAddress,
        new FileLoadingStatusUpdate(id, status),
        cancellationToken);
    response.EnsureSuccessStatusCode();
}
