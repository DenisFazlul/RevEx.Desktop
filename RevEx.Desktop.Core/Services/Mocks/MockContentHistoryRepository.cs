using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using static RevEx.Desktop.Core.Services.MockApiData;

namespace RevEx.Desktop.Core.Services;

internal sealed class MockContentHistoryRepository : IContentHistoryRepository
{
    public Task<IReadOnlyCollection<ContentHistoryEventDto>> GetAllAsync(
        int contentId,
        CancellationToken cancellationToken = default) =>
        FromCollection(ContentHistory.Where(item => item.ContentId == contentId), cancellationToken);

    public Task<ContentHistoryEventDto> AddAsync(
        int contentId,
        CreateContentHistoryEventDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var item = new ContentHistoryEventDto(
            ContentHistory.Count == 0 ? 1 : ContentHistory.Max(entry => entry.Id) + 1,
            contentId,
            DateTimeOffset.UtcNow,
            request.Description,
            "mock-user");
        ContentHistory.Add(item);
        return Task.FromResult(item);
    }
}
