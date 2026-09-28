using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using RevEx.Connector.Contracts;
using RevEx.LoadingQueue;

namespace RevEx.Desktop.Connectors;

public sealed class DesktopConnectorServer(
    IConnectorRegistry registry,
    IFileLoadingQueue loadingQueue,
    Uri address) : IAsyncDisposable
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

        app.MapPost("/api/v1/loading/status", (FileLoadingStatusUpdate update) =>
        {
            try
            {
                loadingQueue.UpdateStatus(update.Id, update.Status);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return Results.BadRequest(new RevitApiErrorResponse(
                    new RevitApiError("INVALID_LOADING_STATUS", exception.Message)));
            }
        });

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
