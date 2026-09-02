namespace RevEx.Desktop.Core.Domain;

public sealed record ContentVersionDto(
    int Id,
    int ContentId,
    IReadOnlyCollection<int> FileIds,
    string Name,
    DateTimeOffset Date,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
