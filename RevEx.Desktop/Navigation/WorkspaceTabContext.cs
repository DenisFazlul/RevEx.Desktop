using System;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.Navigation;

public sealed record WorkspaceTabContext(
    Action<WorkspaceTabRequest> OpenTab,
    Action<WorkspaceTabViewModel> CloseTab);
