using System;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.Navigation;

public interface IWorkspaceTabFactory
{
    WorkspaceTabViewModel Create(Type tabType, WorkspaceTabContext context, object? parameter = null);
}
