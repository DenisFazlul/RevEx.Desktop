namespace RevEx.Desktop.Core.Domain;

public sealed class ContentQueryDto
{
    public int[] CategoryIds { get; init; } = [];
    public string? Name { get; init; }
}
