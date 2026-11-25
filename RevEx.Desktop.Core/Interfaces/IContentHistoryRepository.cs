using RevEx.Domain.Contracts.ContentHistory;

namespace RevEx.Desktop.Core.Interfaces;

public interface IContentHistoryRepository
{
    Task<bool> IsSubscribedAsync(int contentId, CancellationToken cancellationToken = default);
    Task SetSubscriptionAsync(int contentId, bool enabled, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ContentHistoryEventDto>> GetAllAsync(
        int contentId,
        CancellationToken cancellationToken = default);

    Task<ContentHistoryEventDto> AddAsync(
        int contentId,
        CreateContentHistoryEventDto request,
        CancellationToken cancellationToken = default);
}
