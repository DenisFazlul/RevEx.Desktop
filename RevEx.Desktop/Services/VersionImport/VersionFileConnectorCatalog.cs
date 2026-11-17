using System;
using System.Collections.Generic;
using System.Linq;
using RevEx.Connector.Client;
using RevEx.Connector.Contracts;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Services.VersionImport;

public sealed class VersionFileConnectorCatalog
{
    private readonly IConnectorRegistry _registry;
    private readonly IConnectorClient _client;
    private readonly DesktopFileConnector _desktopConnector;

    public VersionFileConnectorCatalog(
        IConnectorRegistry registry,
        IConnectorClient client,
        DesktopFileConnector desktopConnector)
    {
        _registry = registry;
        _client = client;
        _desktopConnector = desktopConnector;
    }

    public event EventHandler? Changed
    {
        add
        {
            _registry.Changed += value;
        }
        remove
        {
            _registry.Changed -= value;
        }
    }

    public IReadOnlyCollection<IVersionFileConnector> GetAll(Platform platform)
    {
        var connectors = _registry.GetAll()
            .Where(SupportsReading)
            .Select(descriptor => (IVersionFileConnector)new RemoteVersionFileConnector(descriptor, _client))
            .Where(connector => connector.SupportsPlatform(platform))
            .OrderBy(connector => connector.Name)
            .ToList();

        if (_desktopConnector.SupportsPlatform(platform))
        {
            connectors.Add(_desktopConnector);
        }

        return connectors;
    }

    private static bool SupportsReading(ConnectorDescriptor connector)
    {
        var canRead = connector.Commands.Any(command => command.Name == ConnectorCommand.ReadFiles);
        var canPrepare = connector.Commands.Any(command =>
            command.Name == ConnectorCommand.PrepareContentVersion);
        return canRead && canPrepare;
    }
}
