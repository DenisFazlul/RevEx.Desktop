namespace RevEx.Desktop.Core.Domain;

public sealed record TagGroupDto(
    int Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class CreateTagGroupDto
{
    public string Name { get; init; } = string.Empty;
}

public sealed class UpdateTagGroupDto
{
    public string Name { get; init; } = string.Empty;
}
