namespace RevEx.Desktop.Core.Domain;

public sealed record TagDto(
    int Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
