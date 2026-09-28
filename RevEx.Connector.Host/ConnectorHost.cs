using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RevEx.Connector.Contracts;

namespace RevEx.Connector.Host;

public sealed class ConnectorHost(ConnectorHostOptions options) : IAsyncDisposable
{
    private WebApplication? _application;
    private readonly HttpClient _registrationClient = new();

    public event Func<LoadContentVersionRequest, CancellationToken, Task<LoadContentVersionResponse>>?
        ContentVersionLoadRequested;
    public event EventHandler? PingReceived;

    public Guid InstanceId { get; } = options.InstanceId ?? Guid.NewGuid();
    public Uri? BaseAddress { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_application is not null)
            throw new InvalidOperationException("Connector Host уже запущен.");

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
        var app = builder.Build();

        app.MapGet("/api/v1/connector/health", () =>
        {
            PingReceived?.Invoke(this, EventArgs.Empty);
            return new ConnectorHealthResponse("ready", InstanceId, options.ActiveDocument);
        });

        app.MapPost("/api/v1/projects/active/content-versions/load", async (
            LoadContentVersionRequest request,
            CancellationToken token) =>
        {
            var handler = ContentVersionLoadRequested;
            return handler is null
                ? Results.Problem("Обработчик загрузки версии не зарегистрирован.", statusCode: 503)
                : Results.Ok(await handler(request, token));
        });

        await app.StartAsync(cancellationToken);
        _application = app;

        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses;
        BaseAddress = new Uri(addresses?.Single()
            ?? throw new InvalidOperationException("Connector Host не сообщил адрес HTTP-сервера."));

        await RegisterAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_application is null)
            return;

        try
        {
            var unregisterUri = new Uri(
                options.DesktopRegistrationAddress,
                $"api/v1/connectors/{InstanceId}");
            await _registrationClient.DeleteAsync(unregisterUri, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Desktop может быть уже закрыт. Остановка Connector не должна из-за этого падать.
        }

        await _application.StopAsync(cancellationToken);
        await _application.DisposeAsync();
        _application = null;
    }

    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var request = new ConnectorRegistrationRequest(
            InstanceId,
            options.Name,
            options.Product,
            options.ProductVersion,
            BaseAddress!.AbsoluteUri,
            Environment.ProcessId,
            options.ActiveDocument);

        var registrationUri = new Uri(options.DesktopRegistrationAddress, "api/v1/connectors/register");
        using var response = await _registrationClient.PostAsJsonAsync(
            registrationUri,
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _registrationClient.Dispose();
    }
}
