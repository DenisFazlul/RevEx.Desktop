namespace RevEx.Desktop.Core.Domain;

public sealed class UpdateContentDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public int? ContentStatusId { get; init; }
    public IReadOnlyCollection<int>? TagIds { get; init; }
}
