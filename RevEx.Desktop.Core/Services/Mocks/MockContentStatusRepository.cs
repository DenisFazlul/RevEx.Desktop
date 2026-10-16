using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using static RevEx.Desktop.Core.Services.MockApiData;

namespace RevEx.Desktop.Core.Services;

internal sealed class MockContentStatusRepository : IContentStatusRepository
{
    public Task<IReadOnlyCollection<ContentStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        FromCollection(ContentStatuses, cancellationToken);

    public Task<ContentStatusDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(ContentStatuses.FirstOrDefault(item => item.Id == id), cancellationToken);
}
