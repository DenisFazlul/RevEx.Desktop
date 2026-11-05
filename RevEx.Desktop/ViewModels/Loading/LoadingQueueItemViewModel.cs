using System;
using RevEx.Domain.Contracts.Connectors;
using RevEx.LoadingQueue;

namespace RevEx.Desktop.ViewModels.Loading;

/// <summary>Модель строки очереди. Форматирует состояние, сообщение и время обновления задачи.</summary>
public sealed class LoadingQueueItemViewModel(ConnectorTaskQueueItem item)
{
    public Guid Id => item.Id;
    public string Name => item.Name;
    public string? Message => item.Message;
    public ConnectorTaskStatus Status => item.Status;
    public string StatusText => item.Status switch
    {
        ConnectorTaskStatus.Sent => "Отправлено",
        ConnectorTaskStatus.Accepted => "Принято",
        ConnectorTaskStatus.Done => "Готово",
        _ => item.Status.ToString()
    };
    public string UpdatedAtText => item.UpdatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
}
