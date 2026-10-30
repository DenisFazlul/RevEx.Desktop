using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RevEx.Desktop.Core.Services;

public sealed class RevExApiTransport(HttpClient httpClient)
{
    internal async Task<bool> GetSubscriptionAsync(int contentId, CancellationToken token) =>
        await httpClient.GetFromJsonAsync<bool>($"api/content/{contentId}/subscription", token);

    internal Task<byte[]> DownloadAsync(string route, CancellationToken cancellationToken) =>
        httpClient.GetByteArrayAsync(route, cancellationToken);

    internal async Task<TResponse> UploadFileAsync<TResponse>(
        string route,
        Stream content,
        string fileName,
        string mediaType,
        string? role,
        CancellationToken cancellationToken)
    {
        using var multipartContent = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        multipartContent.Add(fileContent, "File", fileName);

        if (role is not null)
            multipartContent.Add(new StringContent(role), "Role");

        using var response = await httpClient.PostAsync(route, multipartContent, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"API returned an empty response for '{route}'.");
    }

    internal async Task<IReadOnlyCollection<T>> GetAllAsync<T>(
        string route,
        CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<T[]>(route, cancellationToken);
        return result ?? throw new HttpRequestException($"API returned an empty response for '{route}'.");
    }

    internal async Task<T?> GetByIdAsync<T>(
        string route,
        int id,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"{route}/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"API returned an empty response for '{route}/{id}'.");
    }

    internal async Task<TResponse> PostAsJsonAsync<TRequest, TResponse>(
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(route, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"API returned an empty response for '{route}'.");
    }

    internal async Task PutAsJsonAsync<TRequest>(
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync(route, request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    internal async Task DeleteAsync(string route, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(route, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
