using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using RevEx.Connector.Contracts;
using RevEx.Desktop.ViewModels.Tabs;
using RevEx.LoadingQueue;

namespace RevEx.Desktop.ViewModels.Loading;

public sealed class LoadingQueueTabViewModel : WorkspaceTabViewModel
{
    private readonly IFileLoadingQueue _queue;

    public ObservableCollection<LoadingQueueItemViewModel> Items { get; } = [];
    public bool HasItems => Items.Count > 0;
    public bool HasNoItems => !HasItems;

    public LoadingQueueTabViewModel(IFileLoadingQueue queue)
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

    private void OnQueueChanged(object? sender, EventArgs eventArgs) =>
        Dispatcher.UIThread.Post(Refresh);

    private void Refresh()
    {
        Items.Clear();
        foreach (var item in _queue.GetAll())
            Items.Add(new LoadingQueueItemViewModel(item));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasNoItems));
    }
}

public sealed class LoadingQueueItemViewModel(FileLoadingQueueItem item)
{
    public Guid Id => item.Id;
    public string FileName => item.FileName;
    public FileLoadingStatus Status => item.Status;
    public string StatusText => item.Status switch
    {
        FileLoadingStatus.Accepted => "Принят",
        FileLoadingStatus.Started => "Загрузка начата",
        FileLoadingStatus.Completed => "Загрузка завершена",
        _ => item.Status.ToString()
    };
    public string UpdatedAtText => item.UpdatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
}
