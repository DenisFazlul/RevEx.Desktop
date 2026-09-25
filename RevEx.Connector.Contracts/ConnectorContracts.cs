namespace RevEx.Connector.Contracts;

public enum ConnectorProduct
{
    Revit,
    AutoCad,
    Mock
}

public sealed record ConnectorDescriptor(
    Guid InstanceId,
    string Name,
    ConnectorProduct Product,
    string ProductVersion,
    Uri BaseAddress,
    int ProcessId,
    string? ActiveDocument);

public sealed record ConnectorRegistrationRequest(
    Guid InstanceId,
    string Name,
    ConnectorProduct Product,
    string ProductVersion,
    string BaseAddress,
    int ProcessId,
    string? ActiveDocument);

public sealed record ConnectorRegistrationResponse(bool Accepted);

public sealed record ConnectorHealthResponse(
    string Status,
    Guid InstanceId,
    string? ActiveDocument);
