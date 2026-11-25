using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Core.Interfaces;

public interface IVersionImportRepository
{
    Task<ContentVersionDto> ImportAsync(
        VersionImportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record VersionImportRequest(
    Guid Cid,
    Guid Vid,
    string ContentName,
    string? Description,
    int? CategoryId,
    string VersionName,
    DateTimeOffset Date,
    string ApplicationVersion,
    string PrimaryFilePath,
    string PrimaryFileName,
    string MetadataJson,
    IReadOnlyCollection<VersionImportAuxiliaryFile> AuxiliaryFiles,
    Platform Platform,
    string Application);

public sealed record VersionImportAuxiliaryFile(string Path, string Name, string? MetadataJson = null);
