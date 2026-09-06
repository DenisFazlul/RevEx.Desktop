using System;
using System.Collections.Generic;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.Navigation;

public sealed class WorkspaceTabFactory : IWorkspaceTabFactory
{
    private readonly IReadOnlyDictionary<Type, Func<WorkspaceTabContext, WorkspaceTabViewModel>> _factories;

    public WorkspaceTabFactory(IRevExApiService revExApiService)
    {
        _factories = new Dictionary<Type, Func<WorkspaceTabContext, WorkspaceTabViewModel>>
        {
            [typeof(CatalogTabViewModel)] = context =>
                new CatalogTabViewModel(revExApiService, context.OpenContent)
        };
    }

    public WorkspaceTabViewModel Create(Type tabType, WorkspaceTabContext context)
    {
        if (!_factories.TryGetValue(tabType, out var createTab))
            throw new InvalidOperationException($"Фабрика вкладки {tabType.Name} не зарегистрирована.");

        return createTab(context);
    }
}
