using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RevEx.Connector.Contracts;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Services.VersionImport;

internal sealed class RemoteVersionFileConnector(
    ConnectorDescriptor descriptor,
    IConnectorClient client) : IVersionFileConnector
{
    public string Id => descriptor.InstanceId.ToString("D");

    public string Name => $"{descriptor.Name} ({descriptor.ProductVersion})";

    public ConnectorDescriptor RemoteDescriptor => descriptor;

    public bool SupportsPlatform(Platform platform)
    {
        return descriptor.SupportedPlatforms.Contains(platform);
    }

    public async Task<IReadOnlyCollection<ConnectorFileMetadata>> ReadAsync(
        IReadOnlyCollection<ContentVersionExecuteDto> files,
        CancellationToken cancellationToken = default)
    {
        var task = new ConnectorTask(Guid.NewGuid(), "Чтение файлов версии", ConnectorCommand.ReadFiles);
        var request = new ConnecterReadFileRequest(files);
        var response = await client.ExecuteAsync<ConnecterReadFileRequest, ConnectorReadFilesResponse>(
            descriptor,
            task,
            request,
            cancellationToken);
        return response.Files;
    }

    public async Task<IReadOnlyCollection<ContentVersionExecuteDto>> PrepareAsync(
        Guid cid,
        Guid vid,
        Platform platform,
        IReadOnlyCollection<ContentVersionExecuteDto> files,
        CancellationToken cancellationToken = default)
    {
        if (!descriptor.Commands.Any(command => command.Name == ConnectorCommand.PrepareContentVersion))
        {
            throw new InvalidOperationException(
                $"Коннектор «{Name}» не поддерживает подготовку файлов перед публикацией версии.");
        }

        var task = new ConnectorTask(
            Guid.NewGuid(),
            "Подготовка файлов версии",
            ConnectorCommand.PrepareContentVersion);
        var request = new PrepareContentVersionRequest(cid, vid, platform, files);
        var response = await client.ExecuteAsync<PrepareContentVersionRequest, PrepareContentVersionResponse>(
            descriptor,
            task,
            request,
            cancellationToken);
        if (response.Cid != cid || response.Vid != vid)
        {
            throw new InvalidOperationException("Коннектор вернул другие CID или VID после подготовки файлов.");
        }

        return response.Files;
    }
}
