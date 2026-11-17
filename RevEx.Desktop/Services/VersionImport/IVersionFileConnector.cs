using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Services.VersionImport;

/// <summary>
/// Reads files for the version import workflow. Implementations may use an external transport
/// or run entirely inside Desktop.
/// </summary>
public interface IVersionFileConnector
{
    string Id { get; }

    string Name { get; }

    ConnectorDescriptor? RemoteDescriptor { get; }

    bool SupportsPlatform(Platform platform);

    Task<IReadOnlyCollection<ConnectorFileMetadata>> ReadAsync(
        IReadOnlyCollection<ContentVersionExecuteDto> files,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContentVersionExecuteDto>> PrepareAsync(
        Guid cid,
        Guid vid,
        Platform platform,
        IReadOnlyCollection<ContentVersionExecuteDto> files,
        CancellationToken cancellationToken = default);
}
