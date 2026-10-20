using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class ContentHistoryRepository(RevExApiTransport transport) : IContentHistoryRepository
{
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
