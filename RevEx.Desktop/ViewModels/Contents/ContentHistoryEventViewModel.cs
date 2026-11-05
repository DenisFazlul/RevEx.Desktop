using RevEx.Domain.Contracts.ContentHistory;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Модель записи истории. Подготавливает описание, автора и местное время события.</summary>
public sealed class ContentHistoryEventViewModel(ContentHistoryEventDto value)
{
    public ContentHistoryEventDto Value { get; } = value;
    public string OccurredAtText => Value.OccurredAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
    public string Description => Value.Description;
    public string Author => Value.Author;
}
