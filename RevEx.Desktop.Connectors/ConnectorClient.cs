using System.Net.Http.Json;
using RevEx.Connector.Contracts;

namespace RevEx.Desktop.Connectors;

public sealed class ConnectorClient(HttpClient httpClient) : IConnectorClient
{
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
        Uri statusCallbackAddress,
        CancellationToken cancellationToken = default)
    {
        await GetHealthAsync(connector, cancellationToken);
        return await PostAsync<LoadContentVersionRequest, LoadContentVersionResponse>(
            connector,
            "api/v1/projects/active/content-versions/load",
            new LoadContentVersionRequest(files, statusCallbackAddress),
            cancellationToken);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        ConnectorDescriptor connector,
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            new Uri(connector.BaseAddress, route),
            request,
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
