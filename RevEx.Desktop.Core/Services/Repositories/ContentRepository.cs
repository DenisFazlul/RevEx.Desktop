using RevEx.Domain.Contracts.Content;
using RevEx.Desktop.Core.Interfaces;
using System.Globalization;

namespace RevEx.Desktop.Core.Services;

public sealed class ContentRepository(RevExApiTransport transport) : IContentRepository
{
    public Task<IReadOnlyCollection<ContentParameterDto>> GetParametersAsync(int contentId, CancellationToken token = default)
    {
        return transport.GetAllAsync<ContentParameterDto>($"api/content/{contentId}/parameters", token);
    }

    public Task<ContentParameterDto> CreateParameterAsync(int contentId, ContentParameterInput input, CancellationToken token = default)
    {
        return transport.PostAsJsonAsync<ContentParameterInput, ContentParameterDto>(
            $"api/content/{contentId}/parameters", input, token);
    }

    public Task UpdateParameterAsync(int contentId, int id, ContentParameterInput input, CancellationToken token = default)
    {
        return transport.PutAsJsonAsync($"api/content/{contentId}/parameters/{id}", input, token);
    }

    public Task DeleteParameterAsync(int contentId, int id, CancellationToken token = default)
    {
        return transport.DeleteAsync($"api/content/{contentId}/parameters/{id}", token);
    }

    public Task<Guid> GenerateUuid7Async(CancellationToken cancellationToken = default)
    {
        return transport.PostAsJsonAsync<object, Guid>("api/identifiers/uuid-v7", new object(), cancellationToken);
    }

    public Task<IReadOnlyCollection<ContentDto>> GetAllAsync(
        ContentQueryDto query,
        CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<ContentDto>(BuildContentQueryRoute(query), cancellationToken);

    public Task SetFrozenAsync(int id, bool isFrozen, CancellationToken cancellationToken = default)
    {
        return transport.PutAsJsonAsync($"api/content/{id}/freeze", isFrozen, cancellationToken);
    }

    public Task<ContentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        transport.GetByIdAsync<ContentDto>("api/content", id, cancellationToken);

    public async Task<ContentDto?> GetByCidAsync(Guid cid, CancellationToken cancellationToken = default)
    {
        var matches = await transport.GetAllAsync<ContentDto>(
            $"api/content/query?Cid={cid:D}",
            cancellationToken);
        return matches.SingleOrDefault();
    }

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

        if (query.Cid is Guid cid)
        {
            parameters.Add($"Cid={cid:D}");
        }

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
