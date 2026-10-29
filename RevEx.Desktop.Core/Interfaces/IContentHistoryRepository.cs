using RevEx.Domain.Contracts.ContentHistory;

namespace RevEx.Desktop.Core.Interfaces;

public interface IContentHistoryRepository
{
    Task<IReadOnlyCollection<ContentHistoryEventDto>> GetAllAsync(
        int contentId,
        CancellationToken cancellationToken = default);

    Task<ContentHistoryEventDto> AddAsync(
        int contentId,
        CreateContentHistoryEventDto request,
        CancellationToken cancellationToken = default);
}
