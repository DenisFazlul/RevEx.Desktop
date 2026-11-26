using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class RevExApiService(
    IContentRepository contents,
    IContentSetRepository contentSets,
    ICategoryRepository categories,
    ITagRepository tags,
    ITagGroupRepository tagGroups,
    IContentStatusRepository contentStatuses,
    IContentVersionRepository contentVersions,
    IContentFileRepository contentFiles,
    IContentHistoryRepository contentHistory,
    IVersionImportRepository rfaImports) : IRevExApiService
{
    public IContentSetRepository ContentSets { get; } = contentSets;
    public IContentRepository Contents { get; } = contents;
    public ICategoryRepository Categories { get; } = categories;
    public ITagRepository Tags { get; } = tags;
    public ITagGroupRepository TagGroups { get; } = tagGroups;
    public IContentStatusRepository ContentStatuses { get; } = contentStatuses;
    public IContentVersionRepository ContentVersions { get; } = contentVersions;
    public IContentFileRepository ContentFiles { get; } = contentFiles;
    public IContentHistoryRepository ContentHistory { get; } = contentHistory;
    public IVersionImportRepository VersionImports { get; } = rfaImports;
}
