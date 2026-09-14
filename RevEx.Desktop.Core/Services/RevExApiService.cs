using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class RevExApiService(HttpClient httpClient) : IRevExApiService
{
    public Task<IReadOnlyCollection<ContentDto>> GetContentsAsync(
        ContentQueryDto query,
        CancellationToken cancellationToken = default) =>
        GetAllAsync<ContentDto>(BuildContentQueryRoute(query), cancellationToken);

    public Task<ContentDto?> GetContentAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync<ContentDto>("api/content", id, cancellationToken);

    public Task<ContentDto> CreateContentAsync(
        CreateContentDto request,
        CancellationToken cancellationToken = default) =>
        PostAsJsonAsync<CreateContentDto, ContentDto>("api/content", request, cancellationToken);

    public Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        GetAllAsync<CategoryDto>("api/categories", cancellationToken);

    public Task<CategoryDto?> GetCategoryAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync<CategoryDto>("api/categories", id, cancellationToken);

    public Task<IReadOnlyCollection<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default) =>
        GetAllAsync<TagDto>("api/tags", cancellationToken);

    public Task<TagDto?> GetTagAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync<TagDto>("api/tags", id, cancellationToken);

    public Task<IReadOnlyCollection<ContentStatusDto>> GetContentStatusesAsync(
        CancellationToken cancellationToken = default) =>
        GetAllAsync<ContentStatusDto>("api/content-statuses", cancellationToken);

    public Task<ContentStatusDto?> GetContentStatusAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync<ContentStatusDto>("api/content-statuses", id, cancellationToken);

    public Task<IReadOnlyCollection<ContentVersionDto>> GetContentVersionsAsync(
        CancellationToken cancellationToken = default) =>
        GetAllAsync<ContentVersionDto>("api/content-versions", cancellationToken);

    public Task<ContentVersionDto?> GetContentVersionAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync<ContentVersionDto>("api/content-versions", id, cancellationToken);

    public Task<ContentVersionDto> CreateContentVersionAsync(
        CreateContentVersionDto request,
        CancellationToken cancellationToken = default) =>
        PostAsJsonAsync<CreateContentVersionDto, ContentVersionDto>(
            "api/content-versions",
            request,
            cancellationToken);

    public async Task<ContentVersionDto> UploadContentVersionFileAsync(
        int versionId,
        Stream content,
        string fileName,
        string role,
        CancellationToken cancellationToken = default)
    {
        using var multipartContent = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        multipartContent.Add(fileContent, "File", fileName);
        multipartContent.Add(new StringContent(role), "Role");

        using var response = await httpClient.PostAsync(
            $"api/content-versions/{versionId}/file",
            multipartContent,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContentVersionDto>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("API returned an empty response after uploading the Revit file.");
    }

    public Task<IReadOnlyCollection<ContentFileDto>> GetContentFilesAsync(
        CancellationToken cancellationToken = default) =>
        GetAllAsync<ContentFileDto>("api/content-files", cancellationToken);

    public Task<ContentFileDto?> GetContentFileAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync<ContentFileDto>("api/content-files", id, cancellationToken);

    private static string BuildContentQueryRoute(ContentQueryDto query)
    {
        var parameters = query.CategoryIds
            .Distinct()
            .Select(id => $"CategoryIds={id.ToString(CultureInfo.InvariantCulture)}")
            .ToList();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            parameters.Add($"Name={Uri.EscapeDataString(query.Name.Trim())}");
        }

        return parameters.Count == 0
            ? "api/content/query"
            : $"api/content/query?{string.Join("&", parameters)}";
    }

    private async Task<IReadOnlyCollection<T>> GetAllAsync<T>(
        string route,
        CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<T[]>(route, cancellationToken);
        return result ?? throw new HttpRequestException($"API returned an empty response for '{route}'.");
    }

    private async Task<T?> GetByIdAsync<T>(
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

    private async Task<TResponse> PostAsJsonAsync<TRequest, TResponse>(
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(route, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException($"API returned an empty response for '{route}'.");
    }
}
