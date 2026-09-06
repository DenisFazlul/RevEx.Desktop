using System;

namespace RevEx.Desktop.Navigation;

public sealed record WorkspaceTabContext(Action<WorkspaceTabRequest> OpenTab);
