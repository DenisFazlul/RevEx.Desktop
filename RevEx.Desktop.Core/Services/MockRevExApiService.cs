using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class MockRevExApiService : IRevExApiService
{
    public IContentSetRepository ContentSets { get; }
    public IContentRepository Contents { get; } = new MockContentRepository();
    public ICategoryRepository Categories { get; } = new MockCategoryRepository();
    public ITagRepository Tags { get; } = new MockTagRepository();
    public ITagGroupRepository TagGroups { get; } = new MockTagGroupRepository();
    public IContentStatusRepository ContentStatuses { get; } = new MockContentStatusRepository();
    public IContentVersionRepository ContentVersions { get; } = new MockContentVersionRepository();
    public IContentFileRepository ContentFiles { get; } = new MockContentFileRepository();
    public IContentHistoryRepository ContentHistory { get; } = new MockContentHistoryRepository();
    public IVersionImportRepository VersionImports { get; }

    public MockRevExApiService(Func<string>? currentAuthor = null)
    {
        ContentSets = new MockContentSetRepository(Contents, currentAuthor);
        VersionImports = new CompositeVersionImportRepository(Contents, ContentVersions, ContentFiles);
    }
}
