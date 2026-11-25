using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RevEx.Connector.Client;
using RevEx.Connector.Client.Implementations.Tcp;
using RevEx.Connector.Protocol.Implementations.Tcp;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Services;
using RevEx.Desktop.Views.Contents;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;
using RevEx.Domain.Identifiers;
using RevEx.LoadingQueue;

internal static class VersionPublicationChecks
{
    public static void Run()
    {
        CheckAsync().GetAwaiter().GetResult();
        Console.WriteLine("PASS: publication status selector, all three states, persistence, unchanged version fields, retry, busy state and read-only access.");
    }

    private static async Task CheckAsync()
    {
        var mock = new MockRevExApiService();
        var repository = new StatusRepository(mock.ContentVersions);
        var api = new RevExApiService(mock.Contents, mock.Categories, mock.Tags, mock.TagGroups,
            mock.ContentStatuses, repository, mock.ContentFiles, mock.ContentHistory,
            new CompositeVersionImportRepository(mock.Contents, repository, mock.ContentFiles));
        var category = (await api.Categories.GetAllAsync()).First();
        var content = await api.Contents.CreateAsync(new CreateContentDto
        {
            Cid = Uuid7.Create(),
            Name = "Status check",
            Description = "Status check",
            CategoryId = category.Id
        });
        var version = await api.ContentVersions.CreateAsync(new CreateContentVersionDto
        {
            ContentId = content.Id,
            Name = "Version",
            Platform = Platform.Revit,
            PublishStatus = PublishStatus.Draft,
            ApplicationVersion = "2026"
        });
        using var transport = new TcpConnectorTransport();
        using var client = new TcpConnectorClient(transport, new ConnectorTaskQueue());
        var registry = new ConnectorRegistry();
        var import = new RfaFamilyImportService(api, client, registry);
        using var item = new ContentItemViewModel(content);
        using var model = new ContentDetailsViewModel(item, api, client, registry, import, true,
            () => { }, (_, _, _) => { });
        await model.ActivateAsync();
        var view = new ContentDetailsView { DataContext = model };
        var window = new Window { Content = view, Width = 1200, Height = 900 };
        window.Show();
        view.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        var selector = view.GetVisualDescendants().OfType<ComboBox>()
            .Single(box => ReferenceEquals(box.ItemsSource, model.PublishStatuses));
        var save = view.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == "Сохранить статус");
        Require(model.PublishStatuses.Select(status => status.Name).SequenceEqual(new[] { "Черновик", "Готов", "Снят" }),
            "The publication selector must offer exactly the three requested Russian labels.");
        Require(!save.IsEffectivelyEnabled && model.SelectedPublishStatus?.Value == PublishStatus.Draft,
            "The selector did not reflect the current status.");
        var otherPlatformVersion = version with { Id = -100, Platform = Platform.AutoCad, Name = "AutoCAD check" };
        model.Versions.Add(otherPlatformVersion);
        model.SelectedVersionPlatform = model.VersionPlatforms.Single(option => option.Value == Platform.AutoCad);
        Require(model.CanAddVersion, "The unified version form is unavailable for AutoCAD.");
        while (model.IsLoadingVersion)
        {
            await Task.Delay(1);
        }
        Require(model.FilteredVersions.Count == 1 && model.FilteredVersions[0].Id == -100
            && model.SelectedVersion?.Id == -100, "Platform selection did not filter and select the matching version.");
        model.SelectedVersionPlatform = model.VersionPlatforms.Single(option => option.Value == Platform.Other);
        Require(model.CanAddVersion, "The generic file version form is unavailable for Other.");
        Require(model.HasNoPlatformVersions && model.SelectedVersion is null && model.VersionFiles.Count == 0
            && !model.VersionFamilyMetadata.HasFamily, "An empty platform retained stale version details.");
        model.SelectedVersionPlatform = model.VersionPlatforms.Single(option => option.Value == Platform.Revit);
        Require(model.CanAddVersion, "The RFA version form is unavailable for Revit.");
        while (model.IsLoadingVersion)
        {
            await Task.Delay(1);
        }
        model.Versions.Remove(otherPlatformVersion);
        Require(model.SelectedVersion?.Id == version.Id && model.FilteredVersions.All(item => item.Platform == Platform.Revit),
            "Returning to Revit did not restore a matching version.");
        Dispatcher.UIThread.RunJobs();

        foreach (var status in new[] { PublishStatus.Ready, PublishStatus.Withdrawn, PublishStatus.Draft })
        {
            selector.SelectedItem = model.PublishStatuses.Single(option => option.Value == status);
            Dispatcher.UIThread.RunJobs();
            Require(save.IsEffectivelyEnabled && model.SavePublishStatusCommand.CanExecute(null), "Changing the selector did not enable Save.");
            await model.SavePublishStatusCommand.ExecuteAsync(null);
            var stored = await api.ContentVersions.GetByIdAsync(version.Id);
            Require(stored?.PublishStatus == status && model.SelectedVersion?.PublishStatus == status
                && model.Versions.Single().PublishStatus == status, "The new status was not persisted or displayed.");
            Require(stored?.Platform == version.Platform && stored.Date == version.Date && stored.Name == version.Name
                && stored.ApplicationVersion == version.ApplicationVersion, "Status editing changed other version fields.");
            Require(!model.CanSavePublishStatus, "An unchanged status can be saved again.");
        }

        selector.SelectedItem = model.PublishStatuses.Single(option => option.Value == PublishStatus.Ready);
        repository.FailNextStatusUpdate = true;
        await model.SavePublishStatusCommand.ExecuteAsync(null);
        Require(model.SelectedVersion?.PublishStatus == PublishStatus.Draft && model.CanSavePublishStatus
            && model.VersionErrorMessage is not null, "A failed update lost the current status or prevented retry.");
        repository.StatusGate = new TaskCompletionSource<bool>();
        var pending = model.SavePublishStatusCommand.ExecuteAsync(null);
        Require(model.IsSavingPublishStatus && model.IsVersionBusy && !model.CanChangePublishStatus
            && !model.CanSavePublishStatus, "Status saving did not block concurrent editing.");
        repository.StatusGate.SetResult(true);
        await pending;
        Require(model.SelectedVersion?.PublishStatus == PublishStatus.Ready && model.VersionErrorMessage is null,
            "Retry did not save the selected status.");
        window.Close();

        using var administrator = new ContentDetailsViewModel(item, api, client, registry, import, true,
            () => { }, (_, _, _) => { }, canChangeFreeze: true);
        await administrator.ActivateAsync();
        await administrator.ToggleFreezeCommand.ExecuteAsync(null);
        Require(administrator.IsFrozen && !administrator.CanEdit && !administrator.CanChangePublishStatus,
            "Freezing did not disable edits for the administrator.");
        await model.ActivateAsync();
        Require(!model.IsFrozen && model.CanEdit, "Freezing changed an already open card.");

        using var reopened = new ContentDetailsViewModel(item, api, client, registry, import, true,
            () => { }, (_, _, _) => { });
        await reopened.ActivateAsync();
        Require(reopened.IsFrozen && !reopened.CanEdit && !reopened.CanLoadVersionIntoProject
            && !reopened.ToggleFreezeCommand.CanExecute(null), "A newly opened frozen card is not read-only.");

        var frozenView = new ContentDetailsView { DataContext = reopened };
        var frozenWindow = new Window { Content = frozenView, Width = 1200, Height = 900 };
        frozenWindow.Show();
        frozenView.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        var frozenButtons = frozenView.GetVisualDescendants().OfType<Button>().ToArray();
        foreach (var label in new[] { "Добавить", "Сохранить статус", "Загрузить в проект" })
        {
            Require(!frozenButtons.Single(button => button.Content as string == label).IsEffectivelyEnabled,
                $"The frozen card enabled the {label} action.");
        }

        frozenWindow.Close();

        await administrator.ToggleFreezeCommand.ExecuteAsync(null);
        Require(!administrator.IsFrozen && administrator.CanEdit, "Thawing did not restore editing.");
        using var reopenedAfterThaw = new ContentDetailsViewModel(item, api, client, registry, import, true,
            () => { }, (_, _, _) => { });
        await reopenedAfterThaw.ActivateAsync();
        Require(!reopenedAfterThaw.IsFrozen && reopenedAfterThaw.CanEdit, "Thawing did not persist.");

        using var readOnly = new ContentDetailsViewModel(item, api, client, registry, import, false,
            () => { }, (_, _, _) => { });
        await readOnly.ActivateAsync();
        readOnly.SelectedPublishStatus = readOnly.PublishStatuses.Single(option => option.Value == PublishStatus.Withdrawn);
        Require(!readOnly.CanChangePublishStatus && !readOnly.SavePublishStatusCommand.CanExecute(null),
            "Read-only users can change publication status.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class StatusRepository(IContentVersionRepository inner) : IContentVersionRepository
    {
        public bool FailNextStatusUpdate { get; set; }
        public TaskCompletionSource<bool>? StatusGate { get; set; }

        public async Task UpdatePublishStatusAsync(int versionId, PublishStatus publishStatus, CancellationToken cancellationToken = default)
        {
            if (FailNextStatusUpdate)
            {
                FailNextStatusUpdate = false;
                throw new HttpRequestException("Simulated failure.");
            }

            if (StatusGate is not null)
            {
                await StatusGate.Task;
            }

            await inner.UpdatePublishStatusAsync(versionId, publishStatus, cancellationToken);
        }

        public Task<IReadOnlyCollection<ContentVersionDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return inner.GetAllAsync(cancellationToken);
        }

        public Task<ContentVersionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return inner.GetByIdAsync(id, cancellationToken);
        }

        public Task<ContentVersionDto> CreateAsync(CreateContentVersionDto request, CancellationToken cancellationToken = default)
        {
            return inner.CreateAsync(request, cancellationToken);
        }

        public Task<ContentVersionDto> UploadFileAsync(int versionId, Stream content, string fileName, string role,
            CancellationToken cancellationToken = default, string? metadataJson = null)
        {
            return inner.UploadFileAsync(versionId, content, fileName, role, cancellationToken, metadataJson);
        }

        public Task DeleteFileAsync(int versionId, int fileId, CancellationToken cancellationToken = default)
        {
            return inner.DeleteFileAsync(versionId, fileId, cancellationToken);
        }
    }
}
