namespace RevEx.Desktop.Core.Domain;

public sealed class CreateCategoryDto
{
    public string Name { get; init; } = string.Empty;
}

public sealed class UpdateCategoryDto
{
    public string Name { get; init; } = string.Empty;
}
