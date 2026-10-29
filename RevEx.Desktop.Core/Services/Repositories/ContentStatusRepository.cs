using RevEx.Domain.Contracts.ContentStatus;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class ContentStatusRepository(RevExApiTransport transport) : IContentStatusRepository
{
    public Task<IReadOnlyCollection<ContentStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<ContentStatusDto>("api/content-statuses", cancellationToken);

    public Task<ContentStatusDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        transport.GetByIdAsync<ContentStatusDto>("api/content-statuses", id, cancellationToken);
}
