namespace RevEx.Desktop.Core.Domain;

public sealed record CategoryDto(
    int Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);