using System;
using System.Collections.Generic;
using System.Text.Json;
using RevEx.Domain.Contracts.Connectors;

namespace RevEx.Desktop.Services;

public sealed record DesktopControlRegistration(
    Guid InstanceId,
    int ProcessId,
    string Address,
    string AccessToken,
    DateTimeOffset StartedAtUtc);

public sealed record DesktopControlHealth(
    Guid InstanceId,
    int ProcessId,
    DateTimeOffset StartedAtUtc);

public sealed record DesktopConnectorDescription(
    Guid InstanceId,
    string Name,
    string Product,
    string ProductVersion,
    int ProcessId,
    string? ActiveDocument,
    IReadOnlyCollection<DesktopConnectorCommandDescription> Commands);

public sealed record DesktopConnectorCommandDescription(
    string Name,
    string Description,
    bool RequiresExclusiveExecution,
    ConnectorPayloadContract Input,
    ConnectorPayloadContract Output);

public sealed record ExecuteDesktopConnectorCommandRequest(
    string? TaskName,
    JsonElement Payload);
