using RevEx.Desktop.Core.Interfaces;
using RevEx.Domain.Contracts.ContentSet;

namespace RevEx.Desktop.Core.Services;

public sealed class MockContentSetRepository(IContentRepository contents, Func<string>? currentAuthor = null) : IContentSetRepository
{
    private readonly Dictionary<int, ContentSetDto> _sets = [];
    private readonly HashSet<(int SetId, int ContentId)> _items = [];
    private int _nextId;

    public Task<IReadOnlyCollection<ContentSetDto>> GetAllAsync(CancellationToken token = default)
    {
        return Task.FromResult<IReadOnlyCollection<ContentSetDto>>(_sets.Values.ToArray());
    }

    public Task<ContentSetDto> CreateAsync(SaveContentSetDto request, CancellationToken token = default)
    {
        var now = DateTimeOffset.UtcNow;
        var item = new ContentSetDto(++_nextId, request.Name, request.Description, currentAuthor?.Invoke() ?? "Демо", now, now);
        _sets.Add(item.Id, item);
        return Task.FromResult(item);
    }

    public Task UpdateAsync(int id, SaveContentSetDto request, CancellationToken token = default)
    {
        _sets[id] = _sets[id] with { Name = request.Name, Description = request.Description, UpdatedAt = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken token = default)
    {
        _sets.Remove(id);
        _items.RemoveWhere(item => item.SetId == id);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<ContentSetContentDto>> GetContentsAsync(int id, CancellationToken token = default)
    {
        var all = await contents.GetAllAsync(new RevEx.Domain.Contracts.Content.ContentQueryDto(), token);
        return all.Where(item => _items.Contains((id, item.Id)))
            .Select(item => new ContentSetContentDto(item.Id, item.Name, item.Description)).ToArray();
    }

    public Task AddContentAsync(int id, int contentId, CancellationToken token = default)
    {
        _items.Add((id, contentId));
        return Task.CompletedTask;
    }

    public Task RemoveContentAsync(int id, int contentId, CancellationToken token = default)
    {
        _items.Remove((id, contentId));
        return Task.CompletedTask;
    }
}
