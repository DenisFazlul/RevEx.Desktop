using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using RevEx.Connector.Contracts;

namespace RevEx.Desktop.Connectors;

public sealed class DesktopConnectorServer(IConnectorRegistry registry, Uri address) : IAsyncDisposable
{
    private WebApplication? _application;

    public Uri Address { get; } = address;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_application is not null)
            return;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, Address.Port));
        var app = builder.Build();

        app.MapPost("/api/v1/connectors/register", (ConnectorRegistrationRequest request) =>
        {
            if (!Uri.TryCreate(request.BaseAddress, UriKind.Absolute, out var baseAddress) ||
                baseAddress.Scheme != Uri.UriSchemeHttp ||
                !baseAddress.IsLoopback)
            {
                return Results.BadRequest(new RevitApiErrorResponse(
                    new RevitApiError("INVALID_CONNECTOR_ADDRESS", "Connector передал недопустимый адрес.")));
            }

            registry.Register(new ConnectorDescriptor(
                request.InstanceId,
                request.Name,
                request.Product,
                request.ProductVersion,
                baseAddress,
                request.ProcessId,
                request.ActiveDocument));

            return Results.Ok(new ConnectorRegistrationResponse(true));
        });

        app.MapDelete("/api/v1/connectors/{instanceId:guid}", (Guid instanceId) =>
            registry.Remove(instanceId) ? Results.NoContent() : Results.NotFound());

        await app.StartAsync(cancellationToken);
        _application = app;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_application is null)
            return;
        await _application.StopAsync(cancellationToken);
        await _application.DisposeAsync();
        _application = null;
    }

    public ValueTask DisposeAsync() => new(StopAsync());
}
