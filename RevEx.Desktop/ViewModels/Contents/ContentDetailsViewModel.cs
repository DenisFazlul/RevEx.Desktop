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
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentDetailsViewModel : Tabs.WorkspaceTabViewModel
{
    private readonly Action _openVersionEditor;
    private readonly Action<ContentVersionDto> _openVersionDetails;
    private readonly Action _close;
    private readonly Action<string, string, int> _contentUpdated;
    private readonly IRevExApiService _apiService;
    private ContentDto? _content;
    private IReadOnlyCollection<TagDto> _allTags = [];
    private IReadOnlyCollection<CategoryDto> _categories = [];

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSavingTags;
    [ObservableProperty] private bool _isUpdatingPreview;
    [ObservableProperty] private bool _isSavingContent;
    [ObservableProperty] private bool _isDeleting;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _tagMessage;
    [ObservableProperty] private string? _previewMessage;
    [ObservableProperty] private string? _contentMessage;
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string _categoryName = string.Empty;
    [ObservableProperty] private Bitmap? _previewImage;

    public int Id { get; }
    public ObservableCollection<ContentVersionDto> Versions { get; } = [];
    public ObservableCollection<ContentTagItemViewModel> Tags { get; } = [];
    public IReadOnlyCollection<TagDto> AvailableTags =>
        _allTags.Where(tag => Tags.All(assigned => assigned.Id != tag.Id)).OrderBy(tag => tag.Name).ToArray();
    public bool HasVersions => Versions.Count > 0;
    public bool HasNoVersions => !HasVersions && !IsLoading;
    public bool HasTags => Tags.Count > 0;
    public bool HasNoTags => !HasTags && !IsLoading;
    public bool HasPreview => PreviewImage is not null;
    public bool HasNoPreview => !HasPreview && !IsLoading;

    public ContentDetailsViewModel(
        ContentItemViewModel content,
        IRevExApiService apiService,
        Action openVersionEditor,
        Action<ContentVersionDto> openVersionDetails,
        Action close,
        Action<string, string, int> contentUpdated)
        : base(
            content.Name,
            true,
            content.Id)
    {
        _apiService = apiService;
        _openVersionEditor = openVersionEditor;
        _openVersionDetails = openVersionDetails;
        _close = close;
        _contentUpdated = contentUpdated;
        Id = content.Id;
        _name = content.Name;
        _description = content.Description;
    }

    public override async Task ActivateAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            TagMessage = null;
            var contentTask = _apiService.GetContentAsync(Id);
            var versionsTask = _apiService.GetContentVersionsAsync();
            var tagsTask = _apiService.GetTagsAsync();
            var categoriesTask = _apiService.GetCategoriesAsync();
            await Task.WhenAll(contentTask, versionsTask, tagsTask, categoriesTask);

            var content = await contentTask
                ?? throw new HttpRequestException($"Контент {Id} не найден.");
            ApplyContent(content, await versionsTask, await tagsTask, await categoriesTask);
            await LoadPreviewAsync(content.PreviewFileId);

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

    public async Task AssignTagAsync(TagDto tag)
    {
        if (_content is null || Tags.Any(item => item.Id == tag.Id))
            return;

        await RunTagActionAsync(async () =>
        {
            var tagIds = Tags.Select(item => item.Id).Append(tag.Id).ToArray();
            await UpdateTagAssignmentsAsync(tagIds);
            Tags.Add(new ContentTagItemViewModel(tag, RemoveTagAsync));
            TagMessage = $"Тег «{tag.Name}» добавлен.";
            NotifyCollectionStateChanged();
        });
    }

    public async Task<IReadOnlyCollection<TagDto>> GetAvailableTagsAsync()
    {
        try
        {
            ErrorMessage = null;
            _allTags = await _apiService.GetTagsAsync();
            NotifyCollectionStateChanged();
            return AvailableTags;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось загрузить доступные теги: {exception.Message}";
            return [];
        }
    }

    public void AddOrUpdateVersion(ContentVersionDto version)
    {
        var existing = Versions.FirstOrDefault(item => item.Id == version.Id);
        if (existing is not null)
            Versions.Remove(existing);

        Versions.Insert(0, version);
        NotifyCollectionStateChanged();
    }

    public void OpenVersion(ContentVersionDto version) => _openVersionDetails(version);

    public async Task UpdatePreviewAsync(string path, string fileName)
    {
        if (_content is null)
            return;

        try
        {
            IsUpdatingPreview = true;
            ErrorMessage = null;
            PreviewMessage = "Загружаем изображение…";
            await using var stream = File.OpenRead(path);
            _content = await _apiService.UploadContentPreviewAsync(Id, stream, fileName);
            await LoadPreviewAsync(_content.PreviewFileId);
            PreviewMessage = "Изображение обновлено.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Не удалось обновить изображение: {exception.Message}";
            PreviewMessage = null;
        }
        finally
        {
            IsUpdatingPreview = false;
        }
    }

    public async Task UpdateContentInformationAsync(string name, string description)
    {
        if (_content is null)
            return;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        {
            ErrorMessage = "Название и описание не должны быть пустыми.";
            return;
        }

        try
        {
            IsSavingContent = true;
            ErrorMessage = null;
            ContentMessage = null;
            name = name.Trim();
            description = description.Trim();
            await _apiService.UpdateContentAsync(Id, CreateUpdateRequest(name, description, _content.TagIds));
            _content = _content with { Name = name, Description = description, UpdatedAt = DateTimeOffset.UtcNow };
            Name = name;
            Description = description;
            Title = name;
            _contentUpdated(name, description, _content.CategoryId);
            ContentMessage = "Изменения сохранены.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось сохранить контент: {exception.Message}";
        }
        finally
        {
            IsSavingContent = false;
        }
    }

    public Task UpdateContentNameAsync(string name) =>
        UpdateContentInformationAsync(name, Description);

    public Task UpdateContentDescriptionAsync(string description) =>
        UpdateContentInformationAsync(Name, description);

    public IReadOnlyCollection<CategoryDto> GetCategories() => _categories;
    public int? GetCurrentCategoryId() => _content?.CategoryId;

    public async Task UpdateContentCategoryAsync(CategoryDto category)
    {
        if (_content is null || _content.CategoryId == category.Id)
            return;

        try
        {
            IsSavingContent = true;
            ErrorMessage = null;
            ContentMessage = null;
            await _apiService.UpdateContentAsync(
                Id,
                CreateUpdateRequest(_content.Name, _content.Description, _content.TagIds, category.Id));
            _content = _content with { CategoryId = category.Id, UpdatedAt = DateTimeOffset.UtcNow };
            CategoryName = category.Name;
            _contentUpdated(_content.Name, _content.Description, category.Id);
            ContentMessage = "Категория изменена.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось изменить категорию: {exception.Message}";
        }
        finally
        {
            IsSavingContent = false;
        }
    }

    public async Task DeleteContentAsync()
    {
        try
        {
            IsDeleting = true;
            ErrorMessage = null;
            await _apiService.DeleteContentAsync(Id);
            _close();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось удалить контент: {exception.Message}";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    [RelayCommand]
    private void AddVersion() => _openVersionEditor();

    private async Task RemoveTagAsync(ContentTagItemViewModel tag)
    {
        if (_content is null)
            return;

        await RunTagActionAsync(async () =>
        {
            var tagIds = Tags.Where(item => item.Id != tag.Id).Select(item => item.Id).ToArray();
            await UpdateTagAssignmentsAsync(tagIds);
            Tags.Remove(tag);
            TagMessage = $"Тег «{tag.Name}» снят с контента.";
            NotifyCollectionStateChanged();
        });
    }

    private async Task UpdateTagAssignmentsAsync(IReadOnlyCollection<int> tagIds)
    {
        if (_content is null)
            return;

        await _apiService.UpdateContentAsync(
            Id,
            CreateUpdateRequest(_content.Name, _content.Description, tagIds));
        _content = _content with { TagIds = tagIds, UpdatedAt = DateTimeOffset.UtcNow };
    }

    private async Task RunTagActionAsync(Func<Task> action)
    {
        try
        {
            IsSavingTags = true;
            ErrorMessage = null;
            TagMessage = null;
            await action();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось изменить теги: {exception.Message}";
        }
        finally
        {
            IsSavingTags = false;
        }
    }

    partial void OnIsLoadingChanged(bool value) => NotifyCollectionStateChanged();
    partial void OnPreviewImageChanged(Bitmap? value) => NotifyCollectionStateChanged();

    private async Task LoadPreviewAsync(int? fileId)
    {
        Bitmap? image = null;
        if (fileId is int id)
        {
            var bytes = await _apiService.DownloadContentFileAsync(id);
            using var stream = new MemoryStream(bytes);
            image = new Bitmap(stream);
        }

        var previous = PreviewImage;
        PreviewImage = image;
        previous?.Dispose();
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
            Versions.Add(version);
    }

    private void ReplaceTags(IReadOnlyCollection<int> assignedTagIds)
    {
        Tags.Clear();
        foreach (var tag in _allTags
                     .Where(item => assignedTagIds.Contains(item.Id))
                     .OrderBy(item => item.Name))
            Tags.Add(new ContentTagItemViewModel(tag, RemoveTagAsync));
    }

    private void NotifyCollectionStateChanged()
    {
        OnPropertyChanged(nameof(HasVersions));
        OnPropertyChanged(nameof(HasNoVersions));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(HasNoTags));
        OnPropertyChanged(nameof(AvailableTags));
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(HasNoPreview));
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
