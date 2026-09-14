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
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentEditorTabViewModel : WorkspaceTabViewModel
{
    private readonly IRevExApiService _apiService;
    private readonly ContentItemViewModel? _existingContent;
    private readonly Action<ContentVersionDto>? _versionAdded;
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
    private string? _selectedFilePath;

    [ObservableProperty]
    private string _selectedFileName = "Файл не выбран";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _hasError;

    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public IReadOnlyList<string> RevitVersions { get; } = ["2022", "2023", "2024", "2025", "2026", "2027"];

    public bool IsCreatingContent => _existingContent is null;
    public bool IsAddingVersion => _existingContent is not null;
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
                var categories = await _apiService.GetCategoriesAsync();
                Categories.Clear();
                foreach (var category in categories.OrderBy(item => item.Name))
                    Categories.Add(category);

                SelectedCategory ??= Categories.FirstOrDefault();
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

    public void SetSelectedFile(string path, string fileName)
    {
        SelectedFilePath = path;
        SelectedFileName = fileName;
        Message = null;
        HasError = false;
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

            ContentDto? createdContent = null;
            var contentId = _existingContent?.Id ?? 0;
            if (IsCreatingContent)
            {
                createdContent = await _apiService.CreateContentAsync(new CreateContentDto
                {
                    Name = ContentName.Trim(),
                    Description = Description.Trim(),
                    CategoryId = SelectedCategory!.Id,
                    TagIds = []
                });
                contentId = createdContent.Id;
            }

            var version = await _apiService.CreateContentVersionAsync(new CreateContentVersionDto
            {
                ContentId = contentId,
                Name = VersionName.Trim(),
                Date = DateTimeOffset.UtcNow,
                Status = "draft",
                Application = "revit",
                ApplicationVersion = RevitVersion.Trim()
            });

            await using var fileStream = File.OpenRead(SelectedFilePath!);
            version = await _apiService.UploadContentVersionFileAsync(
                version.Id,
                fileStream,
                SelectedFileName,
                "primary");

            _versionAdded?.Invoke(version);

            if (createdContent is not null)
            {
                ContentName = string.Empty;
                Description = string.Empty;
            }

            SelectedFilePath = null;
            SelectedFileName = "Файл не выбран";
            VersionName = string.Empty;
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

        if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath))
        {
            message = "Выберите существующий RFA-файл.";
            return false;
        }

        if (!string.Equals(Path.GetExtension(SelectedFilePath), ".rfa", StringComparison.OrdinalIgnoreCase))
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
}
