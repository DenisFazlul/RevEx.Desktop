namespace RevEx.Connector.Contracts;

public sealed record InspectFamilyRequest(string Path);

public sealed record InspectFamilyResponse(FamilyInfoDto Family);

public sealed record LoadFamilyRequest(string Path);

public sealed record LoadFamilyResponse(bool Success, FamilyInfoDto Family);

public sealed record FamilyInfoDto(string Name, string Category);

public sealed record RevitApiErrorResponse(RevitApiError Error);

public sealed record RevitApiError(string Code, string Message);
