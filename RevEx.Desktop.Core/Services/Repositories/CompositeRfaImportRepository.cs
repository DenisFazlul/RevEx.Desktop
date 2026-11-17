using RevEx.Desktop.Core.Interfaces;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentFile;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Core.Services;

public sealed class CompositeRfaImportRepository(
    IContentRepository contents,
    IContentVersionRepository versions,
    IContentFileRepository files) : IRfaImportRepository
{
    public async Task<ContentVersionDto> ImportAsync(
        RfaImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var content = await contents.GetByCidAsync(request.Cid, cancellationToken);
        if (content is null)
        {
            if (request.CategoryId is null || string.IsNullOrWhiteSpace(request.Description))
            {
                throw new InvalidOperationException("Для нового контента укажите описание и категорию.");
            }

            content = await contents.CreateAsync(
                new CreateContentDto
                {
                    Cid = request.Cid,
                    Name = request.ContentName,
                    Description = request.Description,
                    CategoryId = request.CategoryId.Value
                },
                cancellationToken);
        }

        var version = await versions.CreateAsync(
            new CreateContentVersionDto
            {
                Cid = request.Cid,
                Vid = request.Vid,
                ContentId = content.Id,
                Name = request.VersionName,
                Date = request.Date,
                Status = "draft",
                PublishStatus = PublishStatus.Draft,
                Application = "revit",
                Platform = Platform.Revit,
                ApplicationVersion = request.ApplicationVersion
            },
            cancellationToken);
        version = await versions.GetByIdAsync(version.Id, cancellationToken)
            ?? throw new InvalidOperationException("Созданная версия больше не существует.");

        var uploaded = new List<ContentFileDto>();
        foreach (var fileId in version.FileIds)
        {
            var file = await files.GetByIdAsync(fileId, cancellationToken);
            if (file is not null)
            {
                uploaded.Add(file);
            }
        }

        if (uploaded.All(file => file.Role != "primary"))
        {
            await using var stream = File.OpenRead(request.PrimaryFilePath);
            version = await versions.UploadFileAsync(
                version.Id,
                stream,
                request.PrimaryFileName,
                "primary",
                cancellationToken,
                request.MetadataJson);
        }

        foreach (var auxiliaryFile in request.AuxiliaryFiles)
        {
            var role = string.Equals(Path.GetExtension(auxiliaryFile.Name), ".csv", StringComparison.OrdinalIgnoreCase)
                ? "lookup-table"
                : "attachment";
            if (uploaded.Any(file =>
                file.Role == role &&
                string.Equals(file.OriginalFileName, auxiliaryFile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            await using var stream = File.OpenRead(auxiliaryFile.Path);
            version = await versions.UploadFileAsync(
                version.Id,
                stream,
                auxiliaryFile.Name,
                role,
                cancellationToken);
        }

        return version;
    }
}
