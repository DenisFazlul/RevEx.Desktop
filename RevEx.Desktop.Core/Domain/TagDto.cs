namespace RevEx.Desktop.Core.Domain;

public sealed record TagDto(
    /* Идентификатор тега. */ int Id,
    /* Название тега. */ string Name,
    /* Дата создания тега. */ DateTimeOffset CreatedAt,
    /* Дата последнего изменения тега. */ DateTimeOffset UpdatedAt);
