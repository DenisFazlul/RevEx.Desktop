namespace RevEx.Desktop.Core.Domain;

public sealed record ContentFileDto(
    int Id,
    string Url,
    int? ContentVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
