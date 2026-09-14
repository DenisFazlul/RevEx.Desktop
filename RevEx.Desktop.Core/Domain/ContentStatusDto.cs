namespace RevEx.Desktop.Core.Domain;

public sealed record ContentStatusDto(
    /* Идентификатор состояния. */ int Id,
    /* Системное название состояния. */ string Name,
    /* Дата создания состояния. */ DateTimeOffset CreatedAt,
    /* Дата последнего изменения состояния. */ DateTimeOffset UpdatedAt);
