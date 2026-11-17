using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Services.VersionImport;

/// <summary>In-process connector for files which do not require a host application.</summary>
public sealed class DesktopFileConnector : IVersionFileConnector
{
    public const string ConnectorId = "desktop-files";

    public string Id => ConnectorId;

    public string Name => "Файлы на этом компьютере";

    public ConnectorDescriptor? RemoteDescriptor => null;

    public bool SupportsPlatform(Platform platform)
    {
        return platform is Platform.AutoCad or Platform.Other;
    }

    public Task<IReadOnlyCollection<ConnectorFileMetadata>> ReadAsync(
        IReadOnlyCollection<ContentVersionExecuteDto> files,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ConnectorFileMetadata>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file.Path);
            if (!info.Exists)
            {
                throw new FileNotFoundException($"Файл «{file.Name}» не найден.", file.Path);
            }

            var metadata = new JObject
            {
                ["FileName"] = info.Name,
                ["Extension"] = info.Extension.ToLowerInvariant(),
                ["SizeBytes"] = info.Length,
                ["CreationTimeUtc"] = info.CreationTimeUtc,
                ["LastWriteTimeUtc"] = info.LastWriteTimeUtc,
                ["Attributes"] = info.Attributes.ToString()
            };
            result.Add(new ConnectorFileMetadata(file, metadata.ToString(Formatting.None)));
        }

        return Task.FromResult<IReadOnlyCollection<ConnectorFileMetadata>>(result);
    }

    public Task<IReadOnlyCollection<ContentVersionExecuteDto>> PrepareAsync(
        Guid cid,
        Guid vid,
        Platform platform,
        IReadOnlyCollection<ContentVersionExecuteDto> files,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(files);
    }
}
