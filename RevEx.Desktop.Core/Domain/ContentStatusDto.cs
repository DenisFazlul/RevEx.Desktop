namespace RevEx.Desktop.Core.Domain;

public sealed record ContentStatusDto(
    int Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
