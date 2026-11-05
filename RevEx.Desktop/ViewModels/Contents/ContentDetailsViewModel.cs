using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentFile;
using RevEx.Domain.Contracts.ContentHistory;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Contracts.Tag;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Connector.Contracts;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Configuration;
using RevEx.Connector.Client;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Модель карточки контента. Хранит данные и состояние экрана; операции разделены по тематическим файлам.</summary>
public partial class ContentDetailsViewModel : Tabs.WorkspaceTabViewModel
{
    private readonly Action _openVersionEditor;
    private readonly Action _close;
    private readonly Action<string, string, int> _contentUpdated;
    private readonly IRevExApiService _apiService;
    private readonly IConnectorClient _connectorClient;
    private readonly IConnectorRegistry _connectorRegistry;
    private ContentDto? _content;
    private IReadOnlyCollection<TagDto> _allTags = [];
    private IReadOnlyCollection<CategoryDto> _categories = [];
    private bool _disposed;

    [ObservableProperty]

    private bool _isSavingTags;
    [ObservableProperty]
    private bool _isUpdatingPreview;
    [ObservableProperty]
    private bool _isSavingContent;
    [ObservableProperty]
    private bool _isDeleting;
    [ObservableProperty]
    private string? _tagMessage;
    [ObservableProperty]
    private string? _previewMessage;
    [ObservableProperty]
    private string? _contentMessage;
    [ObservableProperty]
    private string _name;
    [ObservableProperty]
    private string _description;
    [ObservableProperty]
    private string _categoryName = string.Empty;
    [ObservableProperty]
    private Bitmap? _previewImage;
    [ObservableProperty]
    private ContentVersionDto? _selectedVersion;
    [ObservableProperty]
    private FileRoleOption _selectedFileRole;
    [ObservableProperty]
    private bool _isLoadingVersion;
    [ObservableProperty]
    private bool _isSavingVersionFile;
    [ObservableProperty]
    private string? _versionErrorMessage;
    [ObservableProperty]
    private string? _versionMessage;

    [ObservableProperty]

    private bool _isSubscribed;
    [ObservableProperty]
    private bool _isChangingSubscription;
    [ObservableProperty]
    private string? _subscriptionMessage;
    public string SubscriptionTip => IsSubscribed ? "Отписаться от изменений" : "Подписаться на изменения";
    partial void OnIsSubscribedChanged(bool value) => OnPropertyChanged(nameof(SubscriptionTip));

    [RelayCommand]
    private async Task ToggleSubscriptionAsync()
    {
        if (IsChangingSubscription || IsLoading)
        {
            return;
        }

        IsChangingSubscription = true;
        try
        {
            await _apiService.ContentHistory.SetSubscriptionAsync(Id, !IsSubscribed);
            IsSubscribed = !IsSubscribed;
            SubscriptionMessage = IsSubscribed ? "Вы подписаны на изменения." : "Подписка отменена.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            SubscriptionMessage = $"Не удалось изменить подписку: {exception.Message}";
        }
        finally
        {
            IsChangingSubscription = false;
        }
    }

    public int Id
    {
        get;
    }
    public bool CanEdit
    {
        get;
    }
    public ObservableCollection<ContentVersionDto> Versions { get; } = [];
    public ObservableCollection<ContentHistoryEventViewModel> History { get; } = [];
    public ObservableCollection<ContentVersionFileItemViewModel> VersionFiles { get; } = [];
    public ObservableCollection<ContentTagItemViewModel> Tags { get; } = [];
    public IReadOnlyList<FileRoleOption> FileRoles
    {
        get;
    } =
    [
        new("primary", "Основной файл"),
        new("type-catalog", "Каталог типов TXT"),
        new("lookup-table", "Таблица поиска CSV"),
        new("attachment", "Вложение")
    ];
    public IReadOnlyCollection<TagDto> AvailableTags =>
        _allTags.Where(tag => Tags.All(assigned => assigned.Id != tag.Id)).OrderBy(tag => tag.Name).ToArray();
    public bool HasVersions => Versions.Count > 0;
    public bool HasNoVersions => !HasVersions && !IsLoading;
    public bool HasHistory => History.Count > 0;
    public bool HasNoHistory => !HasHistory && !IsLoading;
    public bool HasSelectedVersion => SelectedVersion is not null;
    public bool HasVersionFiles => VersionFiles.Count > 0;
    public bool HasNoVersionFiles => HasSelectedVersion && !HasVersionFiles && !IsLoadingVersion;
    public bool IsVersionBusy => IsLoadingVersion || IsSavingVersionFile;
    public bool CanLoadVersionIntoProject => HasVersionFiles && !IsVersionBusy;
    public string SelectedVersionDateText => SelectedVersion?.Date.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? string.Empty;
    public string SelectedVersionCreatedAtText => SelectedVersion?.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? string.Empty;
    public string SelectedVersionUpdatedAtText => SelectedVersion?.UpdatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? string.Empty;
    public bool HasTags => Tags.Count > 0;
    public bool HasNoTags => !HasTags && !IsLoading;
    public bool HasPreview => PreviewImage is not null;
    public bool HasNoPreview => !HasPreview && !IsLoading;

    public ContentDetailsViewModel(
        ContentItemViewModel content,
        IRevExApiService apiService,
        IConnectorClient connectorClient,
        IConnectorRegistry connectorRegistry,
        bool canEdit,
        Action openVersionEditor,
        Action close,
        Action<string, string, int> contentUpdated)
        : base(
            content.Name,
            true,
            content.Id)
    {
        _apiService = apiService;
        _connectorClient = connectorClient;
        _connectorRegistry = connectorRegistry;
        CanEdit = canEdit;
        _openVersionEditor = openVersionEditor;
        _close = close;
        _contentUpdated = contentUpdated;
        Id = content.Id;
        _name = content.Name;
        _description = content.Description;
        _selectedFileRole = FileRoles[0];
    }

    public override async Task ActivateAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            TagMessage = null;
            var contentTask = _apiService.Contents.GetByIdAsync(Id);
            var versionsTask = _apiService.ContentVersions.GetAllAsync();
            var tagsTask = _apiService.Tags.GetAllAsync();
            var categoriesTask = _apiService.Categories.GetAllAsync();
            var historyTask = _apiService.ContentHistory.GetAllAsync(Id);
            await Task.WhenAll(contentTask, versionsTask, tagsTask, categoriesTask, historyTask);

            var content = await contentTask
                ?? throw new HttpRequestException($"Контент {Id} не найден.");
            ApplyContent(content, await versionsTask, await tagsTask, await categoriesTask);
            ReplaceHistory(await historyTask);
            await LoadPreviewAsync(content.PreviewFileId);
            await SelectInitialVersionAsync();
            try
            {
                IsSubscribed = await _apiService.ContentHistory.IsSubscribedAsync(Id);
            }
            catch (HttpRequestException exception)
            {
                SubscriptionMessage = $"Не удалось загрузить подписку: {exception.Message}";
            }

            NotifyCollectionStateChanged();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось загрузить данные карточки: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyCollectionStateChanged();
        }
    }

    [RelayCommand]
    private void AddVersion() => _openVersionEditor();

    private async Task RefreshHistoryAsync()
    {
        try
        {
            ReplaceHistory(await _apiService.ContentHistory.GetAllAsync(Id));
            NotifyCollectionStateChanged();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Изменения сохранены, но историю не удалось обновить: {exception.Message}";
        }
    }

    protected override void OnLoadingStateChanged() => NotifyCollectionStateChanged();
    partial void OnPreviewImageChanged(Bitmap? value) => NotifyCollectionStateChanged();
    partial void OnSelectedVersionChanged(ContentVersionDto? value) => NotifyVersionStateChanged();
    partial void OnIsLoadingVersionChanged(bool value) => NotifyVersionStateChanged();
    partial void OnIsSavingVersionFileChanged(bool value) => NotifyVersionStateChanged();

    private async Task LoadPreviewAsync(int? fileId)
    {
        Bitmap? image = null;
        if (fileId is int id)
        {
            var bytes = await _apiService.ContentFiles.DownloadAsync(id);
            using var stream = new MemoryStream(bytes);
            image = new Bitmap(stream);
        }

        var previous = PreviewImage;
        if (_disposed)
        {
            image?.Dispose();
            return;
        }

        PreviewImage = image;
        previous?.Dispose();
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        PreviewImage?.Dispose();
        PreviewImage = null;
        base.Dispose();
    }

    private void ApplyContent(
        ContentDto content,
        IReadOnlyCollection<ContentVersionDto> versions,
        IReadOnlyCollection<TagDto> tags,
        IReadOnlyCollection<CategoryDto> categories)
    {
        _content = content;
        _allTags = tags;
        _categories = categories;
        Name = content.Name;
        Description = content.Description;
        Title = content.Name;
        CategoryName = categories.FirstOrDefault(item => item.Id == content.CategoryId)?.Name
            ?? $"Категория {content.CategoryId}";

        ReplaceVersions(versions);
        ReplaceTags(content.TagIds);
    }

    private void ReplaceVersions(IEnumerable<ContentVersionDto> versions)
    {
        Versions.Clear();
        foreach (var version in versions
                     .Where(item => item.ContentId == Id)
                     .OrderByDescending(item => item.Date))
        {
            Versions.Add(version);
        }
    }

    private void ReplaceHistory(IEnumerable<ContentHistoryEventDto> history)
    {
        History.Clear();
        foreach (var item in history.OrderByDescending(entry => entry.OccurredAt).ThenByDescending(entry => entry.Id))
        {
            History.Add(new ContentHistoryEventViewModel(item));
        }
    }

    private async Task SelectInitialVersionAsync()
    {
        var selected = SelectedVersion is null
            ? Versions.FirstOrDefault()
            : Versions.FirstOrDefault(item => item.Id == SelectedVersion.Id) ?? Versions.FirstOrDefault();

        if (selected is not null)
        {
            await SelectVersionAsync(selected);
        }
        else
        {
            SelectedVersion = null;
            VersionFiles.Clear();
            NotifyVersionStateChanged();
        }
    }

    private void ReplaceVersion(ContentVersionDto version)
    {
        var existing = Versions.FirstOrDefault(item => item.Id == version.Id);
        if (existing is null)
        {
            return;
        }

        var index = Versions.IndexOf(existing);
        Versions[index] = version;
    }

    private void ReplaceVersionFiles(IEnumerable<ContentFileDto> files)
    {
        VersionFiles.Clear();
        foreach (var file in files.OrderBy(item => item.Role).ThenBy(item => item.OriginalFileName))
        {
            VersionFiles.Add(new ContentVersionFileItemViewModel(file, CanEdit));
        }
    }

    private void ReplaceTags(IReadOnlyCollection<int> assignedTagIds)
    {
        Tags.Clear();
        foreach (var tag in _allTags
                     .Where(item => assignedTagIds.Contains(item.Id))
                     .OrderBy(item => item.Name))
        {
            Tags.Add(new ContentTagItemViewModel(tag, RemoveTagAsync, CanEdit));
        }
    }

    private void NotifyCollectionStateChanged()
    {
        OnPropertyChanged(nameof(HasVersions));
        OnPropertyChanged(nameof(HasNoVersions));
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HasNoHistory));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(HasNoTags));
        OnPropertyChanged(nameof(AvailableTags));
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(HasNoPreview));
    }

    private void NotifyVersionStateChanged()
    {
        OnPropertyChanged(nameof(HasSelectedVersion));
        OnPropertyChanged(nameof(HasVersionFiles));
        OnPropertyChanged(nameof(HasNoVersionFiles));
        OnPropertyChanged(nameof(IsVersionBusy));
        OnPropertyChanged(nameof(CanLoadVersionIntoProject));
        OnPropertyChanged(nameof(SelectedVersionDateText));
        OnPropertyChanged(nameof(SelectedVersionCreatedAtText));
        OnPropertyChanged(nameof(SelectedVersionUpdatedAtText));
    }

    private UpdateContentDto CreateUpdateRequest(
        string name,
        string description,
        IReadOnlyCollection<int> tagIds,
        int? categoryId = null) =>
        new()
        {
            Name = name,
            Description = description,
            CategoryId = categoryId ?? _content!.CategoryId,
            ContentStatusId = _content!.ContentStatusId,
            TagIds = tagIds
        };
}
