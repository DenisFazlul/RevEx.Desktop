namespace RevEx.Desktop.Core.Interfaces;

public interface IRevExApiService
{
    IContentRepository Contents { get; }
    ICategoryRepository Categories { get; }
    ITagRepository Tags { get; }
    ITagGroupRepository TagGroups { get; }
    IContentStatusRepository ContentStatuses { get; }
    IContentVersionRepository ContentVersions { get; }
    IContentFileRepository ContentFiles { get; }
    IContentHistoryRepository ContentHistory { get; }
}
