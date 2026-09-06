using System.Threading.Tasks;

namespace RevEx.Desktop.ViewModels.Tabs;

public abstract class WorkspaceTabViewModel(string title, bool canClose, object? key = null) : ViewModelBase
{
    public string Title { get; } = title;
    public bool CanClose { get; } = canClose;
    public object? Key { get; } = key;

    public virtual Task ActivateAsync() => Task.CompletedTask;
}
