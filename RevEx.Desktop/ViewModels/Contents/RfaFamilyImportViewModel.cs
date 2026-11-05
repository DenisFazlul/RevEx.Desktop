using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Services;
using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Contracts.Revit;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class RfaFamilyImportViewModel : ViewModelBase, IDisposable
{
    private readonly IRevExApiService _api;
    private readonly RfaFamilyImportService _import;
    private RfaFamilyReadSession? _session;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave), nameof(CanChooseFile))]
    private bool _isBusy;

    [ObservableProperty]
    private string _fileName = "Файл не выбран";

    [ObservableProperty]
    private string _contentName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private CategoryDto? _selectedCategory;

    [ObservableProperty]
    private string _versionName = string.Empty;

    [ObservableProperty]
    private string _applicationVersion = string.Empty;

    [ObservableProperty]
    private string _connectorName = string.Empty;

    [ObservableProperty]
    private string _familyCategory = string.Empty;

    [ObservableProperty]
    private string _metadataJson = string.Empty;

    [ObservableProperty]
    private string _targetDescription = "Выберите RFA-файл для чтения.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditContent))]
    private bool _isNewContent = true;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _hasError;

    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public ObservableCollection<RevitFamilyReadResultType> FamilyTypes { get; } = [];
    public bool CanSave => !IsBusy && _session is not null && SavedVersion is null;
    public bool CanChooseFile => !IsBusy && _session?.SavedVersion is null && _session?.SavedContent is null;
    public bool CanEditContent => IsNewContent && _session?.SavedContent is null;
    public bool CanEditVersion => _session?.SavedVersion is null;
    public ContentVersionDto? SavedVersion { get; private set; }

    public RfaFamilyImportViewModel(IRevExApiService api, RfaFamilyImportService import)
    {
        _api = api;
        _import = import;
    }

    public async Task LoadFileAsync(string path)
    {
        if (!CanChooseFile)
        {
            return;
        }

        _session?.Dispose();
        _session = null;
        FamilyTypes.Clear();
        ContentName = string.Empty;
        Description = string.Empty;
        SelectedCategory = null;
        MetadataJson = string.Empty;
        ApplicationVersion = string.Empty;
        ConnectorName = string.Empty;
        FamilyCategory = string.Empty;
        VersionName = string.Empty;
        TargetDescription = "Читаем выбранное семейство…";
        FileName = Path.GetFileName(path);
        IsBusy = true;
        HasError = false;
        Message = "Подбираем коннектор и читаем семейство…";
        try
        {
            var categories = await _api.Categories.GetAllAsync();
            Categories.Clear();
            foreach (var category in categories.OrderBy(category => category.Name))
            {
                Categories.Add(category);
            }

            _session = await _import.ReadAsync(path);
            ContentName = _session.ContentName;
            ConnectorName = _session.Connector.Name;
            FamilyCategory = _session.Family.Category ?? string.Empty;
            var year = Regex.Match(_session.Family.ApplicationVersion ?? _session.Connector.ProductVersion, @"20\d{2}");
            ApplicationVersion = year.Success ? year.Value : string.Empty;
            MetadataJson = _session.Metadata.ToString(Formatting.Indented);
            foreach (var type in _session.Family.Types)
            {
                FamilyTypes.Add(type);
            }

            var existing = await _import.FindContentAsync(ContentName);
            IsNewContent = existing is null;
            Description = existing?.Description ?? _session.Family.Description ?? string.Empty;
            if (string.IsNullOrWhiteSpace(Description))
            {
                Description = $"Семейство Revit «{ContentName}».";
            }

            SelectedCategory = existing is null
                ? Categories.FirstOrDefault(category => string.Equals(category.Name, FamilyCategory, StringComparison.OrdinalIgnoreCase))
                : Categories.FirstOrDefault(category => category.Id == existing.CategoryId);
            VersionName = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
            TargetDescription = existing is null
                ? "Будет создана новая карточка контента и первая версия."
                : $"Новая версия будет добавлена к «{existing.Name}» (№ {existing.Id}).";
            Message = "Данные прочитаны. Проверьте поля и сохраните версию.";
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException or OperationCanceledException or JsonException)
        {
            _session?.Dispose();
            _session = null;
            SetError($"Не удалось подготовить семейство: {exception.Message}");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanEditContent));
            OnPropertyChanged(nameof(CanEditVersion));
        }
    }

    public async Task<bool> SaveAsync()
    {
        if (!CanSave || _session is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(VersionName) ||
            !Regex.IsMatch(ApplicationVersion, @"^20\d{2}$"))
        {
            SetError("Укажите название версии и год Revit четырьмя цифрами.");
            return false;
        }

        if (IsNewContent && (SelectedCategory is null || string.IsNullOrWhiteSpace(Description)))
        {
            SetError("Для нового контента выберите категорию и укажите описание.");
            return false;
        }

        IsBusy = true;
        HasError = false;
        Message = "Сохраняем новую версию, файл и метаданные…";
        try
        {
            SavedVersion = await _import.SaveAsync(
                _session, Description, SelectedCategory?.Id, VersionName, ApplicationVersion);
            Message = "Новая версия, файл и метаданные сохранены.";
            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException or OperationCanceledException)
        {
            SetError($"Не удалось сохранить версию: {exception.Message}");
            return false;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanEditContent));
            OnPropertyChanged(nameof(CanEditVersion));
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }

    private void SetError(string message)
    {
        HasError = true;
        Message = message;
    }
}
