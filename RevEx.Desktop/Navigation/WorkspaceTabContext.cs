using System;
using RevEx.Desktop.ViewModels.Contents;

namespace RevEx.Desktop.Navigation;

public sealed record WorkspaceTabContext(Action<ContentItemViewModel> OpenContent);
