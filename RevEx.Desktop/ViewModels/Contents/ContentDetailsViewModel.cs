using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
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
    private readonly IRevExApiService _apiService;
    private ContentDto? _content;
    private IReadOnlyCollection<TagDto> _allTags = [];

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSavingTags;
    [ObservableProperty] private bool _isUpdatingPreview;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _tagMessage;
    [ObservableProperty] private string? _previewMessage;
    [ObservableProperty] private string _description;
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
        Action openVersionEditor)
        : base(
            content.Name,
            true,
            content.Id)
    {
        _apiService = apiService;
        _openVersionEditor = openVersionEditor;
        Id = content.Id;
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
            await Task.WhenAll(contentTask, versionsTask, tagsTask);

            _content = await contentTask
                ?? throw new HttpRequestException($"Контент {Id} не найден.");
            Description = _content.Description;
            _allTags = await tagsTask;
            await LoadPreviewAsync(_content.PreviewFileId);

            Versions.Clear();
            foreach (var version in (await versionsTask)
                         .Where(item => item.ContentId == Id)
                         .OrderByDescending(item => item.Date))
                Versions.Add(version);

            Tags.Clear();
            foreach (var tag in _allTags.Where(tag => _content.TagIds.Contains(tag.Id)).OrderBy(tag => tag.Name))
                Tags.Add(new ContentTagItemViewModel(tag, RemoveTagAsync));

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

        await _apiService.UpdateContentAsync(Id, new UpdateContentDto
        {
            Name = _content.Name,
            Description = _content.Description,
            CategoryId = _content.CategoryId,
            ContentStatusId = _content.ContentStatusId,
            TagIds = tagIds
        });
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
}
