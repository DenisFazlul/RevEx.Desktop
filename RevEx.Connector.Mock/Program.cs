using RevEx.Connector.Contracts;
using RevEx.Connector.Host;

await using var connector = new ConnectorHost(new ConnectorHostOptions(
    "Connector Mock",
    ConnectorProduct.Mock,
    "1.0",
    ConnectorHostOptions.DefaultDesktopRegistrationAddress,
    "Mock Project.rvt"));

connector.InspectRequested += (request, _) =>
{
    Validate(request.Path);
    return Task.FromResult(new InspectFamilyResponse(CreateFamilyInfo(request.Path)));
};

connector.LoadRequested += (request, _) =>
{
    Validate(request.Path);
    return Task.FromResult(new LoadFamilyResponse(true, CreateFamilyInfo(request.Path)));
};

connector.ContentVersionLoadRequested += (request, _) =>
{
    if (request.Paths.Count == 0)
        throw new ArgumentException("Список файлов версии пуст.");

    foreach (var path in request.Paths)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException("Файл версии не найден.", path);
    }

    Console.WriteLine($"Mock-загрузка в проект: {request.Paths.Count} файл(ов)");
    foreach (var path in request.Paths)
        Console.WriteLine($"  {path}");

    return Task.FromResult(new LoadContentVersionResponse(true, request.Paths.Count));
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

static void Validate(string? path)
{
    if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("Путь к семейству не указан.");
    if (!string.Equals(Path.GetExtension(path), ".rfa", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Можно обрабатывать только файлы *.rfa.");
}

static FamilyInfoDto CreateFamilyInfo(string path)
{
    var normalizedPath = path.Replace('\\', '/');
    var name = Path.GetFileNameWithoutExtension(normalizedPath);
    return new FamilyInfoDto(name, GetMockCategory(name));
}

static string GetMockCategory(string familyName)
{
    if (familyName.Contains("door", StringComparison.OrdinalIgnoreCase) ||
        familyName.Contains("двер", StringComparison.OrdinalIgnoreCase))
        return "Doors";
    if (familyName.Contains("window", StringComparison.OrdinalIgnoreCase) ||
        familyName.Contains("окн", StringComparison.OrdinalIgnoreCase))
        return "Windows";
    if (familyName.Contains("chair", StringComparison.OrdinalIgnoreCase) ||
        familyName.Contains("стул", StringComparison.OrdinalIgnoreCase))
        return "Furniture";
    return "Generic Models";
}
