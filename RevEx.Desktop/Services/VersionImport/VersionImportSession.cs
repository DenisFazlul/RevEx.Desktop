using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.Revit;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Services.VersionImport;

public sealed class VersionImportSession : IDisposable
{
    private readonly RfaFamilyReadSession? _revitSession;

    internal VersionImportSession(
        ContentDto content,
        Platform platform,
        IVersionFileConnector connector,
        IReadOnlyCollection<ConnectorFileMetadata> files,
        RfaFamilyReadSession? revitSession = null)
    {
        Content = content;
        Platform = platform;
        Connector = connector;
        Files = files;
        _revitSession = revitSession;
    }

    public ContentDto Content { get; }

    public Platform Platform { get; }

    public IVersionFileConnector Connector { get; }

    public IReadOnlyCollection<ConnectorFileMetadata> Files { get; }

    public RevitFamilyReadResult? RevitFamily => _revitSession?.Family;

    public string SuggestedApplicationVersion
    {
        get
        {
            if (_revitSession is not null)
            {
                return _revitSession.Family.ApplicationVersion ?? _revitSession.Connector.ProductVersion;
            }

            var extension = Files.Count == 1
                ? Path.GetExtension(Files.Single().File.Name).TrimStart('.').ToLowerInvariant()
                : string.Empty;
            return string.IsNullOrWhiteSpace(extension) ? "file" : extension;
        }
    }

    internal RfaFamilyReadSession? RevitSession => _revitSession;

    internal Guid? Vid { get; set; }

    internal IReadOnlyCollection<ContentVersionExecuteDto>? PreparedFiles { get; set; }

    public void Dispose()
    {
        _revitSession?.Dispose();
    }
}
