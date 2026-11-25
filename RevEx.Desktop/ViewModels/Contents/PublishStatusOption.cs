using RevEx.Domain.Entities;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Статус публикации с названием для интерфейса.</summary>
public sealed record PublishStatusOption(PublishStatus Value, string Name)
{
    public static string GetName(PublishStatus status)
    {
        return status switch
        {
            PublishStatus.Draft => "Черновик",
            PublishStatus.Ready => "Готов",
            PublishStatus.Withdrawn => "Снят",
            _ => string.Empty
        };
    }
}
