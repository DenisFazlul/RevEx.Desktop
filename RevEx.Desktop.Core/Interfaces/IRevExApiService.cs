using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.Core.Interfaces;

public interface IRevExApiService
{
    Task<IReadOnlyCollection<ContentDto>> GetContentsAsync(
        ContentQueryDto query,
        CancellationToken cancellationToken = default);
    Task<ContentDto?> GetContentAsync(int id, CancellationToken cancellationToken = default);
    Task<ContentDto> CreateContentAsync(
        CreateContentDto request,
        CancellationToken cancellationToken = default);
    Task UpdateContentAsync(
        int id,
        UpdateContentDto request,
        CancellationToken cancellationToken = default);
    Task<ContentDto> UploadContentPreviewAsync(
        int id,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
    Task<byte[]> DownloadContentFileAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TagGroupDto>> GetTagGroupsAsync(CancellationToken cancellationToken = default);
    Task<TagGroupDto> CreateTagGroupAsync(CreateTagGroupDto request, CancellationToken cancellationToken = default);
    Task UpdateTagGroupAsync(int id, UpdateTagGroupDto request, CancellationToken cancellationToken = default);
    Task DeleteTagGroupAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default);
    Task<TagDto?> GetTagAsync(int id, CancellationToken cancellationToken = default);
    Task<TagDto> CreateTagAsync(CreateTagDto request, CancellationToken cancellationToken = default);
    Task UpdateTagAsync(int id, UpdateTagDto request, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContentStatusDto>> GetContentStatusesAsync(CancellationToken cancellationToken = default);
    Task<ContentStatusDto?> GetContentStatusAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContentVersionDto>> GetContentVersionsAsync(CancellationToken cancellationToken = default);
    Task<ContentVersionDto?> GetContentVersionAsync(int id, CancellationToken cancellationToken = default);
    Task<ContentVersionDto> CreateContentVersionAsync(
        CreateContentVersionDto request,
        CancellationToken cancellationToken = default);
    Task<ContentVersionDto> UploadContentVersionFileAsync(
        int versionId,
        Stream content,
        string fileName,
        string role,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContentFileDto>> GetContentFilesAsync(CancellationToken cancellationToken = default);
    Task<ContentFileDto?> GetContentFileAsync(int id, CancellationToken cancellationToken = default);
}
