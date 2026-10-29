using RevEx.Domain.Contracts.Tag;
using RevEx.Desktop.Core.Interfaces;
using static RevEx.Desktop.Core.Services.MockApiData;

namespace RevEx.Desktop.Core.Services;

internal sealed class MockTagRepository : ITagRepository
{
    public Task<IReadOnlyCollection<TagDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        FromCollection(Tags, cancellationToken);

    public Task<TagDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Tags.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<TagDto> CreateAsync(
        CreateTagDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var group = TagGroups.FirstOrDefault(item => item.Id == request.TagGroupId)
            ?? throw new HttpRequestException($"Tag group {request.TagGroupId} was not found.");
        var tag = new TagDto(Tags.Count == 0 ? 1 : Tags.Max(item => item.Id) + 1,
            request.Name, group.Id, group.Name, now, now);
        Tags.Add(tag);
        return Task.FromResult(tag);
    }

    public Task UpdateAsync(
        int id,
        UpdateTagDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = Tags.FindIndex(item => item.Id == id);
        if (index < 0)
            throw new HttpRequestException($"Tag {id} was not found.");

        var group = TagGroups.FirstOrDefault(item => item.Id == request.TagGroupId)
            ?? throw new HttpRequestException($"Tag group {request.TagGroupId} was not found.");
        Tags[index] = Tags[index] with
        {
            Name = request.Name,
            TagGroupId = group.Id,
            TagGroupName = group.Name,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Tags.RemoveAll(item => item.Id == id);
        for (var index = 0; index < Contents.Count; index++)
        {
            var content = Contents[index];
            Contents[index] = content with
            {
                TagIds = content.TagIds.Where(tagId => tagId != id).ToArray(),
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        return Task.CompletedTask;
    }
}
