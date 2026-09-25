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

    public Task UpdateContentAsync(
        int id,
        UpdateContentDto request,
        CancellationToken cancellationToken = default) =>
        PutAsJsonAsync($"api/content/{id}", request, cancellationToken);

    public Task DeleteContentAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/content/{id}", cancellationToken);

    public async Task<ContentDto> UploadContentPreviewAsync(
        int id,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
        => await UploadFileAsync<ContentDto>(
            $"api/content/{id}/preview",
            content,
            fileName,
            GetImageMediaType(fileName),
            null,
            cancellationToken);

    public Task<byte[]> DownloadContentFileAsync(int id, CancellationToken cancellationToken = default) =>
        httpClient.GetByteArrayAsync($"api/content-files/{id}/content", cancellationToken);

    public Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        GetAllAsync<CategoryDto>("api/categories", cancellationToken);

    public Task<CategoryDto?> GetCategoryAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync<CategoryDto>("api/categories", id, cancellationToken);

    public Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryDto request,
        CancellationToken cancellationToken = default) =>
        PostAsJsonAsync<CreateCategoryDto, CategoryDto>("api/categories", request, cancellationToken);

    public Task UpdateCategoryAsync(
        int id,
        UpdateCategoryDto request,
        CancellationToken cancellationToken = default) =>
        PutAsJsonAsync($"api/categories/{id}", request, cancellationToken);

    public Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/categories/{id}", cancellationToken);

    public Task<IReadOnlyCollection<TagGroupDto>> GetTagGroupsAsync(CancellationToken cancellationToken = default) =>
        GetAllAsync<TagGroupDto>("api/tag-groups", cancellationToken);

    public Task<TagGroupDto> CreateTagGroupAsync(CreateTagGroupDto request, CancellationToken cancellationToken = default) =>
        PostAsJsonAsync<CreateTagGroupDto, TagGroupDto>("api/tag-groups", request, cancellationToken);

    public Task UpdateTagGroupAsync(int id, UpdateTagGroupDto request, CancellationToken cancellationToken = default) =>
        PutAsJsonAsync($"api/tag-groups/{id}", request, cancellationToken);

    public Task DeleteTagGroupAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/tag-groups/{id}", cancellationToken);

    public Task<IReadOnlyCollection<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default) =>
        GetAllAsync<TagDto>("api/tags", cancellationToken);

    public Task<TagDto?> GetTagAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdAsync<TagDto>("api/tags", id, cancellationToken);

    public Task<TagDto> CreateTagAsync(CreateTagDto request, CancellationToken cancellationToken = default) =>
        PostAsJsonAsync<CreateTagDto, TagDto>("api/tags", request, cancellationToken);

    public Task UpdateTagAsync(int id, UpdateTagDto request, CancellationToken cancellationToken = default) =>
        PutAsJsonAsync($"api/tags/{id}", request, cancellationToken);

    public Task DeleteTagAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/tags/{id}", cancellationToken);

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
        => await UploadFileAsync<ContentVersionDto>(
            $"api/content-versions/{versionId}/file",
            content,
            fileName,
            "application/octet-stream",
            role,
            cancellationToken);

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

        parameters.AddRange(query.TagIds
            .Distinct()
            .Select(id => $"TagIds={id.ToString(CultureInfo.InvariantCulture)}"));

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            parameters.Add($"Name={Uri.EscapeDataString(query.Name.Trim())}");
        }

        return parameters.Count == 0
            ? "api/content/query"
            : $"api/content/query?{string.Join("&", parameters)}";
    }

    private static string GetImageMediaType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };

    private async Task<TResponse> UploadFileAsync<TResponse>(
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

    private async Task PutAsJsonAsync<TRequest>(
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PutAsJsonAsync(route, request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteAsync(string route, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(route, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
