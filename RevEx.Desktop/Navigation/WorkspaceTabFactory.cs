using System;
using System.Collections.Generic;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.ViewModels.Settings;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.Navigation;

public sealed class WorkspaceTabFactory : IWorkspaceTabFactory
{
    private readonly IRevExApiService _revExApiService;
    private readonly IAppSettings _settings;
    private readonly IReadOnlyDictionary<Type, Func<WorkspaceTabContext, object?, WorkspaceTabViewModel>> _factories;

    public WorkspaceTabFactory(IRevExApiService revExApiService, IAppSettings settings)
    {
        _revExApiService = revExApiService;
        _settings = settings;
        _factories = new Dictionary<Type, Func<WorkspaceTabContext, object?, WorkspaceTabViewModel>>
        {
            [typeof(CatalogTabViewModel)] = CreateCatalogTab,
            [typeof(ContentDetailsViewModel)] = CreateContentDetailsTab,
            [typeof(SettingsTabViewModel)] = CreateSettings
        };
    }

    private WorkspaceTabViewModel CreateSettings(WorkspaceTabContext arg1, object? arg2)
    {
        return new SettingsTabViewModel(_settings, "settings", true);
    }

    public WorkspaceTabViewModel Create(Type tabType, WorkspaceTabContext context, object? parameter = null)
    {
        if (!_factories.TryGetValue(tabType, out var createTab))
            throw new InvalidOperationException($"Фабрика вкладки {tabType.Name} не зарегистрирована.");

        return createTab(context, parameter);
    }

    private CatalogTabViewModel CreateCatalogTab(WorkspaceTabContext context, object? _) =>
        new(
            _revExApiService,
            content => context.OpenTab(new WorkspaceTabRequest(
                typeof(ContentDetailsViewModel),
                content.Id,
                content)));

    private static ContentDetailsViewModel CreateContentDetailsTab(
        WorkspaceTabContext _,
        object? parameter) =>
        new(GetRequiredParameter<ContentItemViewModel>(parameter));

    private static T GetRequiredParameter<T>(object? parameter) where T : class =>
        parameter as T
        ?? throw new ArgumentException($"Для создания вкладки требуется параметр {typeof(T).Name}.", nameof(parameter));
}
