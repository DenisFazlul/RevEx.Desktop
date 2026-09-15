using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RevEx.Desktop.ViewModels.Tabs;

public abstract partial class WorkspaceTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;

    protected WorkspaceTabViewModel(
        string title,
        bool canClose,
        object? key = null)
    {
        Title = title;
        CanClose = canClose;
        Key = key;
    }

    public string Title { get; }
    public bool CanClose { get; }
    public object? Key { get; }

    public virtual Task ActivateAsync() => Task.CompletedTask;
}
