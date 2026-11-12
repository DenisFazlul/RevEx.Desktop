using System;
using System.Text.Json;

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
    string? ActiveDocument);

public sealed record ExecuteDesktopConnectorCommandRequest(
    string? TaskName,
    JsonElement Payload);
