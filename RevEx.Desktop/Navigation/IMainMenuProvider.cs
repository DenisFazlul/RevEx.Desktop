using System.Collections.Generic;

namespace RevEx.Desktop.Navigation;

public interface IMainMenuProvider
{
    IReadOnlyList<WorkspaceTabDescriptor> Items { get; }
}
