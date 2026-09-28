using System;
using System.Collections.Generic;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Categories;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.ViewModels.Settings;
using RevEx.Desktop.ViewModels.Tabs;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Connector.Contracts;
using RevEx.Desktop.Connectors;
using RevEx.Desktop.Services;
using RevEx.Configuration;
using RevEx.LoadingQueue;
using RevEx.Desktop.ViewModels.Loading;
using RevEx.Desktop.Core.Services;

namespace RevEx.Desktop.Navigation;

public sealed class WorkspaceTabFactory : IWorkspaceTabFactory
{
    private readonly IRevExApiService _revExApiService;
    private readonly IAppSettings _settings;
    private readonly IConnectorClient _connectorClient;
    private readonly IConnectorRegistry _connectorRegistry;
    private readonly IRevExUserSettingsStore _userSettingsStore;
    private readonly IFileLoadingQueue _fileLoadingQueue;
    private readonly IAuthenticationService _authenticationService;
    private readonly IReadOnlyDictionary<Type, Func<WorkspaceTabContext, object?, WorkspaceTabViewModel>> _factories;

    public WorkspaceTabFactory(
        IRevExApiService revExApiService,
        IConnectorClient connectorClient,
        IConnectorRegistry connectorRegistry,
        IRevExUserSettingsStore userSettingsStore,
        IFileLoadingQueue fileLoadingQueue,
        IAuthenticationService authenticationService,
        IAppSettings settings)
    {
        _revExApiService = revExApiService;
        _connectorClient = connectorClient;
        _connectorRegistry = connectorRegistry;
        _userSettingsStore = userSettingsStore;
        _fileLoadingQueue = fileLoadingQueue;
        _authenticationService = authenticationService;
        _settings = settings;
        _factories = new Dictionary<Type, Func<WorkspaceTabContext, object?, WorkspaceTabViewModel>>
        {
            [typeof(CatalogTabViewModel)] = CreateCatalogTab,
            [typeof(CategoryAdminTabViewModel)] = CreateCategoryAdminTab,
            [typeof(ContentDetailsViewModel)] = CreateContentDetailsTab,
            [typeof(ContentEditorTabViewModel)] = CreateContentEditorTab,
            [typeof(LoadingQueueTabViewModel)] = CreateLoadingQueueTab,
            [typeof(TagAdminTabViewModel)] = CreateTagAdminTab,
            [typeof(SettingsTabViewModel)] = CreateSettings
        };
    }

    private WorkspaceTabViewModel CreateLoadingQueueTab(WorkspaceTabContext _, object? __) =>
        new LoadingQueueTabViewModel(_fileLoadingQueue);

    private WorkspaceTabViewModel CreateSettings(WorkspaceTabContext arg1, object? arg2)
    {
        return new SettingsTabViewModel(
            _settings,
            _connectorRegistry,
            _connectorClient,
            _userSettingsStore,
            "settings",
            true);
    }

    private WorkspaceTabViewModel CreateTagAdminTab(WorkspaceTabContext _, object? __) =>
        new TagAdminTabViewModel(_revExApiService);

    private WorkspaceTabViewModel CreateCategoryAdminTab(WorkspaceTabContext _, object? __) =>
        new CategoryAdminTabViewModel(_revExApiService);

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

    private ContentEditorTabViewModel CreateContentEditorTab(WorkspaceTabContext _, object? parameter) =>
        parameter switch
        {
            ContentVersionEditorParameter versionEditor => new ContentEditorTabViewModel(
                _revExApiService,
                versionEditor.Content,
                versionEditor.VersionAdded),
            null => new ContentEditorTabViewModel(_revExApiService),
            _ => throw new ArgumentException("Неизвестный параметр вкладки добавления контента.", nameof(parameter))
        };

    private ContentDetailsViewModel CreateContentDetailsTab(
        WorkspaceTabContext context,
        object? parameter)
    {
        var content = GetRequiredParameter<ContentItemViewModel>(parameter);
        ContentDetailsViewModel? details = null;
        details = new ContentDetailsViewModel(
            content,
            _revExApiService,
            _connectorClient,
            _connectorRegistry,
            _fileLoadingQueue,
            new Uri(new Uri(_settings.ConnectorRegistrationAddress), "api/v1/loading/status"),
            CanEditContent(),
            () => context.OpenTab(new WorkspaceTabRequest(
                typeof(ContentEditorTabViewModel),
                content.Id,
                new ContentVersionEditorParameter(content, details!.AddOrUpdateVersion))),
            () => context.CloseTab(details!),
            (name, description, categoryId) =>
            {
                content.Name = name;
                content.Description = description;
                content.CategoryId = categoryId;
            });
        return details;
    }

    private bool CanEditContent() =>
        _authenticationService.IsInRole(RevExRoles.Moderator) ||
        _authenticationService.IsInRole(RevExRoles.Administrator);

    private static T GetRequiredParameter<T>(object? parameter) where T : class =>
        parameter as T
        ?? throw new ArgumentException($"Для создания вкладки требуется параметр {typeof(T).Name}.", nameof(parameter));

    private sealed record ContentVersionEditorParameter(
        ContentItemViewModel Content,
        Action<ContentVersionDto> VersionAdded);
}
