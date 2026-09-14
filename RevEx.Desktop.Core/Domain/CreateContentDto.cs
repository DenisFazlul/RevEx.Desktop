namespace RevEx.Desktop.Core.Domain;

public sealed class CreateContentDto
{
    /// <summary>Название создаваемого контента.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Описание назначения и состава контента.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Идентификатор выбранной категории.</summary>
    public int CategoryId { get; init; }

    /// <summary>Необязательный идентификатор состояния; API применит draft по умолчанию.</summary>
    public int? ContentStatusId { get; init; }

    /// <summary>Необязательный набор идентификаторов тегов.</summary>
    public IReadOnlyCollection<int>? TagIds { get; init; }
}
