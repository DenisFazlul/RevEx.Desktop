using RevEx.Domain.Contracts.ContentFile;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class ContentFileRepository(RevExApiTransport transport) : IContentFileRepository
{
    public Task<IReadOnlyCollection<ContentFileDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<ContentFileDto>("api/content-files", cancellationToken);

    public Task<ContentFileDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        transport.GetByIdAsync<ContentFileDto>("api/content-files", id, cancellationToken);

    public Task<byte[]> DownloadAsync(int id, CancellationToken cancellationToken = default) =>
        transport.DownloadAsync($"api/content-files/{id}/content", cancellationToken);
}
