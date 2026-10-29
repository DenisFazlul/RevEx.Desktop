using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class ContentVersionRepository(RevExApiTransport transport) : IContentVersionRepository
{
    public Task<IReadOnlyCollection<ContentVersionDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<ContentVersionDto>("api/content-versions", cancellationToken);

    public Task<ContentVersionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        transport.GetByIdAsync<ContentVersionDto>("api/content-versions", id, cancellationToken);

    public Task<ContentVersionDto> CreateAsync(
        CreateContentVersionDto request,
        CancellationToken cancellationToken = default) =>
        transport.PostAsJsonAsync<CreateContentVersionDto, ContentVersionDto>(
            "api/content-versions",
            request,
            cancellationToken);

    public async Task<ContentVersionDto> UploadFileAsync(
        int versionId,
        Stream content,
        string fileName,
        string role,
        CancellationToken cancellationToken = default)
        => await transport.UploadFileAsync<ContentVersionDto>(
            $"api/content-versions/{versionId}/file",
            content,
            fileName,
            "application/octet-stream",
            role,
            cancellationToken);

    public Task DeleteFileAsync(
        int versionId,
        int fileId,
        CancellationToken cancellationToken = default) =>
        transport.DeleteAsync($"api/content-versions/{versionId}/files/{fileId}", cancellationToken);
}
