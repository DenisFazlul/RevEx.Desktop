using RevEx.Domain.Contracts.ContentVersion;

namespace RevEx.Desktop.Core.Interfaces;

public interface IRfaImportRepository
{
    Task<ContentVersionDto> ImportAsync(
        RfaImportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record RfaImportRequest(
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
    IReadOnlyCollection<RfaImportAuxiliaryFile> AuxiliaryFiles);

public sealed record RfaImportAuxiliaryFile(string Path, string Name);
