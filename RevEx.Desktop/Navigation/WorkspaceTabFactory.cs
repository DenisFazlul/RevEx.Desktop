using System;
using System.Collections.Generic;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.ViewModels.Settings;
using RevEx.Desktop.ViewModels.Tabs;
using RevEx.Desktop.ViewModels.Tags;

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
            [typeof(ContentEditorTabViewModel)] = CreateContentEditorTab,
            [typeof(TagAdminTabViewModel)] = CreateTagAdminTab,
            [typeof(SettingsTabViewModel)] = CreateSettings
        };
    }

    private WorkspaceTabViewModel CreateSettings(WorkspaceTabContext arg1, object? arg2)
    {
        return new SettingsTabViewModel(_settings, "settings", true);
    }

    private WorkspaceTabViewModel CreateTagAdminTab(WorkspaceTabContext _, object? __) =>
        new TagAdminTabViewModel(_revExApiService);

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
            () => context.OpenTab(new WorkspaceTabRequest(
                typeof(ContentEditorTabViewModel),
                content.Id,
                new ContentVersionEditorParameter(content, details!.AddOrUpdateVersion))),
            () => context.CloseTab(details!),
            (name, description) =>
            {
                content.Name = name;
                content.Description = description;
            });
        return details;
    }

    private static T GetRequiredParameter<T>(object? parameter) where T : class =>
        parameter as T
        ?? throw new ArgumentException($"Для создания вкладки требуется параметр {typeof(T).Name}.", nameof(parameter));

    private sealed record ContentVersionEditorParameter(
        ContentItemViewModel Content,
        Action<ContentVersionDto> VersionAdded);
}
