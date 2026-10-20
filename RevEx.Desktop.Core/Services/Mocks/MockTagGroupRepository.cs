using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using static RevEx.Desktop.Core.Services.MockApiData;

namespace RevEx.Desktop.Core.Services;

internal sealed class MockTagGroupRepository : ITagGroupRepository
{
    public Task<IReadOnlyCollection<TagGroupDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        FromCollection(TagGroups, cancellationToken);

    public Task<TagGroupDto> CreateAsync(CreateTagGroupDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var group = new TagGroupDto(TagGroups.Count == 0 ? 1 : TagGroups.Max(item => item.Id) + 1,
            request.Name, now, now);
        TagGroups.Add(group);
        return Task.FromResult(group);
    }

    public Task UpdateAsync(int id, UpdateTagGroupDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = TagGroups.FindIndex(item => item.Id == id);
        if (index < 0) throw new HttpRequestException($"Tag group {id} was not found.");
        TagGroups[index] = TagGroups[index] with { Name = request.Name, UpdatedAt = DateTimeOffset.UtcNow };
        for (var tagIndex = 0; tagIndex < Tags.Count; tagIndex++)
            if (Tags[tagIndex].TagGroupId == id)
                Tags[tagIndex] = Tags[tagIndex] with { TagGroupName = request.Name };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Tags.Any(item => item.TagGroupId == id))
            throw new HttpRequestException("Move or delete all tags in this group first.");
        TagGroups.RemoveAll(item => item.Id == id);
        return Task.CompletedTask;
    }
}
