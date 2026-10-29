using RevEx.Domain.Contracts.ContentStatus;

namespace RevEx.Desktop.Core.Interfaces;

public interface IContentStatusRepository
{
    Task<IReadOnlyCollection<ContentStatusDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ContentStatusDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
