using RevEx.Domain.Contracts.ContentHistory;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class ContentHistoryRepository(RevExApiTransport transport) : IContentHistoryRepository
{
    public Task<bool> IsSubscribedAsync(int contentId, CancellationToken cancellationToken = default) =>
        transport.GetSubscriptionAsync(contentId, cancellationToken);

    public Task SetSubscriptionAsync(int contentId, bool enabled, CancellationToken cancellationToken = default) =>
        enabled ? transport.PutAsJsonAsync($"api/content/{contentId}/subscription", new { }, cancellationToken)
            : transport.DeleteAsync($"api/content/{contentId}/subscription", cancellationToken);

    public Task<IReadOnlyCollection<ContentHistoryEventDto>> GetAllAsync(
        int contentId,
        CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<ContentHistoryEventDto>($"api/content/{contentId}/history", cancellationToken);

    public Task<ContentHistoryEventDto> AddAsync(
        int contentId,
        CreateContentHistoryEventDto request,
        CancellationToken cancellationToken = default) =>
        transport.PostAsJsonAsync<CreateContentHistoryEventDto, ContentHistoryEventDto>(
            $"api/content/{contentId}/history",
            request,
            cancellationToken);
}
