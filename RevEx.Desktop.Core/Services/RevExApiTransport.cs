using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RevEx.Desktop.Core.Services;

public sealed class RevExApiTransport(HttpClient httpClient)
{
    internal async Task<TResponse> PostMultipartAsync<TResponse>(
        string route,
        MultipartFormDataContent content,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(route, content, cancellationToken);
        await EnsureSucceededAsync(response, route, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"API returned an empty response for '{route}'.");
    }

    internal async Task<RevEx.Domain.Contracts.ContentVersion.ContentVersionDto> ImportVersionAsync(
        RevEx.Desktop.Core.Interfaces.VersionImportRequest request,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(request.Cid.ToString("D")), "Cid");
        content.Add(new StringContent(request.Vid.ToString("D")), "Vid");
        content.Add(new StringContent(request.ContentName), "ContentName");
        if (request.Description is not null)
        {
            content.Add(new StringContent(request.Description), "Description");
        }

        if (request.CategoryId is int categoryId)
        {
            content.Add(new StringContent(categoryId.ToString(System.Globalization.CultureInfo.InvariantCulture)), "CategoryId");
        }

        content.Add(new StringContent(request.VersionName), "VersionName");
        content.Add(new StringContent(request.Date.ToString("O")), "Date");
        content.Add(new StringContent(request.Platform.ToString()), "Platform");
        content.Add(new StringContent(request.Application), "Application");
        content.Add(new StringContent(request.ApplicationVersion), "ApplicationVersion");
        content.Add(new StringContent(request.MetadataJson), "MetadataJson");

        var primaryStream = File.OpenRead(request.PrimaryFilePath);
        var primaryContent = new StreamContent(primaryStream);
        primaryContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(primaryContent, "File", request.PrimaryFileName);

        var index = 0;
        foreach (var file in request.AuxiliaryFiles)
        {
            var stream = File.OpenRead(file.Path);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "AuxiliaryFiles", file.Name);
            if (file.MetadataJson is not null)
            {
                content.Add(new StringContent(file.MetadataJson), $"AuxiliaryMetadataJson[{index}]");
            }

            index++;
        }

        return await PostMultipartAsync<RevEx.Domain.Contracts.ContentVersion.ContentVersionDto>(
            "api/imports/versions",
            content,
            cancellationToken);
    }

    internal async Task<bool> GetSubscriptionAsync(int contentId, CancellationToken token)
    {
        var route = $"api/content/{contentId}/subscription";
        using var response = await httpClient.GetAsync(route, token);
        await EnsureSucceededAsync(response, route, token);
        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: token);
    }

    internal async Task<byte[]> DownloadAsync(string route, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(route, cancellationToken);
        await EnsureSucceededAsync(response, route, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    internal async Task<TResponse> UploadFileAsync<TResponse>(
        string route,
        Stream content,
        string fileName,
        string mediaType,
        string? role,
        CancellationToken cancellationToken,
        string? metadataJson = null)
    {
        using var multipartContent = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        multipartContent.Add(fileContent, "File", fileName);

        if (role is not null)
        {
            multipartContent.Add(new StringContent(role), "Role");
        }

        if (metadataJson is not null)
        {
            multipartContent.Add(new StringContent(metadataJson), "MetadataJson");
        }

        using var response = await httpClient.PostAsync(route, multipartContent, cancellationToken);
        await EnsureSucceededAsync(response, route, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"API returned an empty response for '{route}'.");
    }

    internal async Task<IReadOnlyCollection<T>> GetAllAsync<T>(
        string route,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(route, cancellationToken);
        await EnsureSucceededAsync(response, route, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<T[]>(cancellationToken: cancellationToken);
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

        await EnsureSucceededAsync(response, route, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"API returned an empty response for '{route}/{id}'.");
    }

    internal async Task<TResponse> PostAsJsonAsync<TRequest, TResponse>(
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(route, request, cancellationToken);
        await EnsureSucceededAsync(response, route, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"API returned an empty response for '{route}'.");
    }

    private static async Task EnsureSucceededAsync(
        HttpResponseMessage response,
        string route,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var messages = new List<string>();
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType == "application/json" || mediaType == "application/problem+json")
        {
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var kind = ReadString(root, "kind");
                    var problemDetail = ReadString(root, "detail");
                    if (mediaType == "application/problem+json" &&
                        kind is "controlled" or "unexpected" &&
                        !string.IsNullOrWhiteSpace(problemDetail))
                    {
                        throw new RevExApiException(response.StatusCode, kind,
                            ReadString(root, "code"), ReadString(root, "traceId"), problemDetail);
                    }

                    if (response.StatusCode != HttpStatusCode.BadRequest)
                    {
                        response.EnsureSuccessStatusCode();
                    }

                    foreach (var property in new[] { "message", "detail" })
                    {
                        if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { } message)
                        {
                            messages.Add(message);
                        }
                    }

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in errors.EnumerateObject())
                        {
                            if (field.Value.ValueKind != JsonValueKind.Array)
                            {
                                continue;
                            }

                            foreach (var error in field.Value.EnumerateArray())
                            {
                                if (error.ValueKind == JsonValueKind.String)
                                {
                                    messages.Add($"{field.Name}: {error.GetString()}");
                                }
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Fall back to the HTTP status if the error body is malformed.
            }
        }

        var detail = string.Join(" ", messages.Distinct());
        var status = $"HTTP {(int)response.StatusCode} ({route})";
        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(detail) ? status : $"{status}: {detail}",
            null,
            response.StatusCode);
    }

    private static string? ReadString(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    internal async Task PutAsJsonAsync<TRequest>(
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync(route, request, cancellationToken);
        await EnsureSucceededAsync(response, route, cancellationToken);
    }

    internal async Task DeleteAsync(string route, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(route, cancellationToken);
        await EnsureSucceededAsync(response, route, cancellationToken);
    }
}
