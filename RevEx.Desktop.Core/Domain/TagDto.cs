namespace RevEx.Desktop.Core.Domain;

public sealed record TagDto(
    /* Идентификатор тега. */ int Id,
    /* Название тега. */ string Name,
    /* Идентификатор группы. */ int TagGroupId,
    /* Название группы. */ string TagGroupName,
    /* Дата создания тега. */ DateTimeOffset CreatedAt,
    /* Дата последнего изменения тега. */ DateTimeOffset UpdatedAt);
