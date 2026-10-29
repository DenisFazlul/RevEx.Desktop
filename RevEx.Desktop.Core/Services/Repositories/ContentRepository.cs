using RevEx.Domain.Contracts.Content;
using RevEx.Desktop.Core.Interfaces;
using System.Globalization;

namespace RevEx.Desktop.Core.Services;

public sealed class ContentRepository(RevExApiTransport transport) : IContentRepository
{
    public Task<IReadOnlyCollection<ContentDto>> GetAllAsync(
        ContentQueryDto query,
        CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<ContentDto>(BuildContentQueryRoute(query), cancellationToken);

    public Task<ContentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        transport.GetByIdAsync<ContentDto>("api/content", id, cancellationToken);

    public Task<ContentDto> CreateAsync(
        CreateContentDto request,
        CancellationToken cancellationToken = default) =>
        transport.PostAsJsonAsync<CreateContentDto, ContentDto>("api/content", request, cancellationToken);

    public Task UpdateAsync(
        int id,
        UpdateContentDto request,
        CancellationToken cancellationToken = default) =>
        transport.PutAsJsonAsync($"api/content/{id}", request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        transport.DeleteAsync($"api/content/{id}", cancellationToken);

    public async Task<ContentDto> UploadPreviewAsync(
        int id,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
        => await transport.UploadFileAsync<ContentDto>(
            $"api/content/{id}/preview",
            content,
            fileName,
            GetImageMediaType(fileName),
            null,
            cancellationToken);

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
}
