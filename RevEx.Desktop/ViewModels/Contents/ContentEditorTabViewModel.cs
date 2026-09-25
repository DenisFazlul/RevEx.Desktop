using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Configuration;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentEditorTabViewModel : WorkspaceTabViewModel
{
    private const string NoFamilyFileSelected = "Файл не выбран";
    private const string NoPreviewSelected = "Изображение не выбрано";

    private readonly IRevExApiService _apiService;
    private readonly ContentItemViewModel? _existingContent;
    private readonly Action<ContentVersionDto>? _versionAdded;
    private IReadOnlyCollection<TagDto> _availableTags = [];
    private bool _isLoaded;

    [ObservableProperty]
    private CategoryDto? _selectedCategory;

    [ObservableProperty]
    private string _contentName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _versionName = "1.0.0";

    [ObservableProperty]
    private string _revitVersion = "2026";

    [ObservableProperty]
    private string? _selectedFamilyFilePath;

    [ObservableProperty]
    private string _selectedFamilyFileName = NoFamilyFileSelected;

    [ObservableProperty]
    private string? _selectedPreviewPath;

    [ObservableProperty]
    private string _selectedPreviewName = NoPreviewSelected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _hasError;

    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public ObservableCollection<ContentTagItemViewModel> SelectedTags { get; } = [];
    public IReadOnlyList<string> RevitVersions { get; } = ["2022", "2023", "2024", "2025", "2026", "2027"];

    public bool IsCreatingContent => _existingContent is null;
    public bool IsAddingVersion => _existingContent is not null;
    public bool HasSelectedTags => SelectedTags.Count > 0;
    public bool HasNoSelectedTags => !HasSelectedTags;
    public string ExistingContentName => _existingContent?.Name ?? string.Empty;
    public string PageDescription => IsCreatingContent
        ? "Создайте карточку контента и загрузите первое Revit-семейство"
        : $"Добавьте новое Revit-семейство для «{ExistingContentName}»";

    public ContentEditorTabViewModel(
        IRevExApiService apiService,
        ContentItemViewModel? existingContent = null,
        Action<ContentVersionDto>? versionAdded = null)
        : base(
            existingContent is null ? "Добавление контента" : $"Новая версия: {existingContent.Name}",
            true,
            existingContent?.Id)
    {
        _apiService = apiService;
        _existingContent = existingContent;
        _versionAdded = versionAdded;
    }

    public override async Task ActivateAsync()
    {
        if (_isLoaded)
            return;

        try
        {
            IsLoading = true;
            HasError = false;
            Message = null;

            if (IsCreatingContent)
            {
                var categoriesTask = _apiService.GetCategoriesAsync();
                var tagsTask = _apiService.GetTagsAsync();
                await Task.WhenAll(categoriesTask, tagsTask);

                var categories = await categoriesTask;
                Categories.Clear();
                foreach (var category in categories.OrderBy(item => item.Name))
                    Categories.Add(category);

                SelectedCategory ??= Categories.FirstOrDefault();
                _availableTags = await tagsTask;
            }

            _isLoaded = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            SetError($"Не удалось загрузить данные из API: {exception.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetSelectedFamilyFile(string path, string fileName)
    {
        SelectedFamilyFilePath = path;
        SelectedFamilyFileName = fileName;
        ClearMessage();
    }

    public void SetSelectedPreview(string path, string fileName)
    {
        SelectedPreviewPath = path;
        SelectedPreviewName = fileName;
        ClearMessage();
    }

    public IReadOnlyCollection<TagDto> GetAvailableTags() =>
        _availableTags
            .Where(tag => SelectedTags.All(selected => selected.Id != tag.Id))
            .OrderBy(tag => tag.TagGroupName)
            .ThenBy(tag => tag.Name)
            .ToArray();

    public void AddTag(TagDto tag)
    {
        if (SelectedTags.Any(item => item.Id == tag.Id))
            return;

        SelectedTags.Add(new ContentTagItemViewModel(tag, RemoveTagAsync));
        NotifySelectedTagsChanged();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!TryValidate(out var validationMessage))
        {
            SetError(validationMessage);
            return;
        }

        try
        {
            IsSaving = true;
            HasError = false;
            Message = "Сохраняем контент и загружаем Revit-семейство…";

            var createdContent = IsCreatingContent ? await CreateContentWithPreviewAsync() : null;
            var contentId = createdContent?.Id ?? _existingContent!.Id;
            var version = await CreateVersionWithFamilyFileAsync(contentId);

            _versionAdded?.Invoke(version);
            ResetForm(createdContent is not null);
            Message = createdContent is null
                ? "Новая версия успешно добавлена."
                : "Контент и первая версия успешно добавлены.";
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            SetError($"Не удалось сохранить данные: {exception.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool TryValidate(out string message)
    {
        if (IsCreatingContent && string.IsNullOrWhiteSpace(ContentName))
        {
            message = "Укажите название контента.";
            return false;
        }

        if (IsCreatingContent && string.IsNullOrWhiteSpace(Description))
        {
            message = "Добавьте описание контента.";
            return false;
        }

        if (IsCreatingContent && SelectedCategory is null)
        {
            message = "Выберите категорию.";
            return false;
        }


        if (IsCreatingContent &&
            (string.IsNullOrWhiteSpace(SelectedPreviewPath) || !File.Exists(SelectedPreviewPath)))
        {
            message = "Выберите изображение контента.";
            return false;
        }

        if (IsCreatingContent && !ContentImageUploadConfiguration.IsSupported(SelectedPreviewPath!))
        {
            message = "Изображение должно быть в формате JPEG, PNG, WebP или GIF.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(VersionName))
        {
            message = "Укажите название версии.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(RevitVersion) ||
            RevitVersion.Length != 4 ||
            !RevitVersion.All(char.IsDigit))
        {
            message = "Укажите год версии Revit четырьмя цифрами.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedFamilyFilePath) || !File.Exists(SelectedFamilyFilePath))
        {
            message = "Выберите существующий RFA-файл.";
            return false;
        }

        if (!string.Equals(Path.GetExtension(SelectedFamilyFilePath), ".rfa", StringComparison.OrdinalIgnoreCase))
        {
            message = "Основным файлом версии может быть только Revit Family (*.rfa).";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void SetError(string message)
    {
        HasError = true;
        Message = message;
    }

    private void ClearMessage()
    {
        Message = null;
        HasError = false;
    }

    private async Task<ContentDto> CreateContentWithPreviewAsync()
    {
        var content = await _apiService.CreateContentAsync(new CreateContentDto
        {
            Name = ContentName.Trim(),
            Description = Description.Trim(),
            CategoryId = SelectedCategory!.Id,
            TagIds = SelectedTags.Select(tag => tag.Id).ToArray()
        });

        await using var previewStream = File.OpenRead(SelectedPreviewPath!);
        return await _apiService.UploadContentPreviewAsync(
            content.Id,
            previewStream,
            SelectedPreviewName);
    }

    private async Task<ContentVersionDto> CreateVersionWithFamilyFileAsync(int contentId)
    {
        var version = await _apiService.CreateContentVersionAsync(new CreateContentVersionDto
        {
            ContentId = contentId,
            Name = VersionName.Trim(),
            Date = DateTimeOffset.UtcNow,
            Status = "draft",
            Application = "revit",
            ApplicationVersion = RevitVersion.Trim()
        });

        await using var familyStream = File.OpenRead(SelectedFamilyFilePath!);
        return await _apiService.UploadContentVersionFileAsync(
            version.Id,
            familyStream,
            SelectedFamilyFileName,
            "primary");
    }

    private void ResetForm(bool contentWasCreated)
    {
        if (contentWasCreated)
        {
            ContentName = string.Empty;
            Description = string.Empty;
            SelectedPreviewPath = null;
            SelectedPreviewName = NoPreviewSelected;
            SelectedTags.Clear();
            NotifySelectedTagsChanged();
        }

        SelectedFamilyFilePath = null;
        SelectedFamilyFileName = NoFamilyFileSelected;
        VersionName = string.Empty;
    }

    private Task RemoveTagAsync(ContentTagItemViewModel tag)
    {
        SelectedTags.Remove(tag);
        NotifySelectedTagsChanged();
        return Task.CompletedTask;
    }

    private void NotifySelectedTagsChanged()
    {
        OnPropertyChanged(nameof(HasSelectedTags));
        OnPropertyChanged(nameof(HasNoSelectedTags));
    }

}
