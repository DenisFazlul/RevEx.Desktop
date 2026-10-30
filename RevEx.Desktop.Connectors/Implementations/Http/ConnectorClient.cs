using System.Net.Http.Json;
using RevEx.Connector.Contracts;
using RevEx.Domain.Contracts.Connectors;

namespace RevEx.Desktop.Connectors;

public sealed class ConnectorClient(HttpClient httpClient, Uri? statusCallbackAddress = null) : IConnectorClient
{
    public Task<TResponse> ExecuteAsync<TRequest, TResponse>(ConnectorDescriptor connector,
        ConnectorTask task, TRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The archived HTTP protocol does not support generic tasks.");

    public async Task<ConnectorHealthResponse> GetHealthAsync(
        ConnectorDescriptor connector,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ConnectorHealthResponse>(
            new Uri(connector.BaseAddress, "api/v1/connector/health"),
            cancellationToken)
            ?? throw new HttpRequestException("Connector вернул пустой ответ health.");
    }

    public async Task<LoadContentVersionResponse> LoadContentVersionAsync(
        ConnectorDescriptor connector,
        IReadOnlyCollection<ContentVersionLoadFile> files,
        CancellationToken cancellationToken = default)
    {
        await GetHealthAsync(connector, cancellationToken);
        return await PostAsync<LoadContentVersionRequest, LoadContentVersionResponse>(
            connector,
            "api/v1/projects/active/content-versions/load",
            new LoadContentVersionRequest(files, statusCallbackAddress ?? throw new InvalidOperationException("HTTP status callback address is not configured.")),
            cancellationToken);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        ConnectorDescriptor connector,
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        // The in-process .NET Framework listener requires Content-Length, not chunked transfer.
        using var content = JsonContent.Create(request);
        await content.LoadIntoBufferAsync();
        using var response = await httpClient.PostAsync(
            new Uri(connector.BaseAddress, route),
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var apiError = await response.Content.ReadFromJsonAsync<RevitApiErrorResponse>(
                cancellationToken: cancellationToken);
            throw new HttpRequestException(
                apiError?.Error.Message ?? $"Connector вернул код {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"Connector вернул пустой ответ для '{route}'.");
    }
}
