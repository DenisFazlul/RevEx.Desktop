namespace RevEx.Desktop.ViewModels.Tabs;

public abstract class WorkspaceTabViewModel(string title, bool canClose) : ViewModelBase
{
    public string Title { get; } = title;
    public bool CanClose { get; } = canClose;
}
