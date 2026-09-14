namespace RevEx.Desktop.Core.Domain;

public sealed class CreateContentVersionDto
{
    /// <summary>Идентификатор контента, для которого создаётся версия.</summary>
    public int ContentId { get; init; }

    /// <summary>Пользовательское название версии.</summary>
    public string? Name { get; init; }

    /// <summary>Дата выпуска или создания версии.</summary>
    public DateTimeOffset? Date { get; init; }

    /// <summary>Состояние создаваемой версии.</summary>
    public string? Status { get; init; }

    /// <summary>Целевое приложение; в MVP всегда Revit.</summary>
    public string Application { get; init; } = "revit";

    /// <summary>Год версии Revit, в которой сохранено семейство.</summary>
    public string ApplicationVersion { get; init; } = string.Empty;
}
