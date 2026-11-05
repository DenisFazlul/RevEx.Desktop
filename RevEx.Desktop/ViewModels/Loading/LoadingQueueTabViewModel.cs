using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Desktop.ViewModels.Tabs;
using RevEx.LoadingQueue;

namespace RevEx.Desktop.ViewModels.Loading;

/// <summary>Модель вкладки загрузок. Следит за очередью коннекторов и обновляет отображаемые задачи.</summary>
public sealed class LoadingQueueTabViewModel : WorkspaceTabViewModel
{
    private readonly IConnectorTaskQueue _queue;
    private bool _disposed;

    public ObservableCollection<LoadingQueueItemViewModel> Items { get; } = [];
    public bool HasItems => Items.Count > 0;
    public bool HasNoItems => !HasItems;

    public LoadingQueueTabViewModel(IConnectorTaskQueue queue)
        : base("Загрузки", true, "loading-queue")
    {
        _queue = queue;
        _queue.Changed += OnQueueChanged;
        Refresh();
    }

    public override Task ActivateAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _disposed = true;
        _queue.Changed -= OnQueueChanged;
        base.Dispose();
    }

    private void OnQueueChanged(object? sender, EventArgs eventArgs) =>
        Dispatcher.UIThread.Post(Refresh);

    private void Refresh()
    {
        if (_disposed)
        {
            return;
        }

        Items.Clear();
        foreach (var item in _queue.GetAll())
        {
            Items.Add(new LoadingQueueItemViewModel(item));
        }

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasNoItems));
    }
}
