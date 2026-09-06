using System;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.Navigation;

public sealed record WorkspaceTabDescriptor(string Title, Type TabType)
{
    public static WorkspaceTabDescriptor Create<TTab>(string title)
        where TTab : WorkspaceTabViewModel => new(title, typeof(TTab));
}
