using System;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.Navigation;

public sealed record WorkspaceTabDescriptor(string Title, string Symbol, Type TabType)
{
    public static WorkspaceTabDescriptor Create<TTab>(string title, string symbol)
        where TTab : WorkspaceTabViewModel => new(title, symbol, typeof(TTab));
}
