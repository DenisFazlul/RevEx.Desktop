namespace RevEx.Desktop.Core.Domain;

public sealed class CreateTagDto
{
    public string Name { get; init; } = string.Empty;
    public int TagGroupId { get; init; }
}

public sealed class UpdateTagDto
{
    public string Name { get; init; } = string.Empty;
    public int TagGroupId { get; init; }
}
