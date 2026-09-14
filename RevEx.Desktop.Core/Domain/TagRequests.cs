namespace RevEx.Desktop.Core.Domain;

public sealed class CreateTagDto
{
    public string Name { get; init; } = string.Empty;
}

public sealed class UpdateTagDto
{
    public string Name { get; init; } = string.Empty;
}
