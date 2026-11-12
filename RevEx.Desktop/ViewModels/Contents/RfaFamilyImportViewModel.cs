using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.ViewModels.Revit;
using Newtonsoft.Json;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Services;
using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.ContentVersion;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Модель импорта RFA. Читает семейство через коннектор, заполняет поля и сохраняет новую версию с метаданными.</summary>
public partial class RfaFamilyImportViewModel : DialogViewModel<ContentVersionDto>, IDisposable
{
    private readonly IRevExApiService _api;
    private readonly RfaFamilyImportService _import;
    private RfaFamilyReadSession? _session;
    private bool _disposed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChooseFile))]
    private ConnectorDescriptor? _selectedConnector;

    [ObservableProperty]
    private string _fileName = "Файл не выбран";

    [ObservableProperty]
    private string _auxiliaryFileNames = "Вспомогательные файлы не выбраны";

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
    private string _targetDescription = "Выберите коннектор и нажмите «Открыть», чтобы выбрать RFA-файл.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditContent))]
    private bool _isNewContent = true;

    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public ObservableCollection<ConnectorDescriptor> Connectors { get; } = [];
    public RevitFamilyReadResultViewModel FamilyMetadata { get; } = new();
    public bool CanSave => !IsBusy && _session is not null && SavedVersion is null;
    public bool CanChangeConnector => !IsBusy && _session?.SavedVersion is null && _session?.SavedContent is null;
    public bool CanChooseFile => CanChangeConnector && SelectedConnector is not null && Connectors.Contains(SelectedConnector);
    public bool CanEditContent => IsNewContent && _session?.SavedContent is null;
    public bool CanEditVersion => _session?.SavedVersion is null;
    public ContentVersionDto? SavedVersion
    {
        get; private set;
    }

    public RfaFamilyImportViewModel(IRevExApiService api, RfaFamilyImportService import)
    {
        _api = api;
        _import = import;
        _import.ConnectorsChanged += OnConnectorsChanged;
        UpdateConnectors();
    }

    public async Task LoadFileAsync(string path)
    {
        await LoadFilesAsync([path]);
    }

    public async Task LoadFilesAsync(IReadOnlyCollection<string> paths)
    {
        var connector = SelectedConnector;
        if (!CanChooseFile || connector is null)
        {
            return;
        }

        var selectedPaths = paths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var families = selectedPaths.Where(path =>
            string.Equals(Path.GetExtension(path), ".rfa", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (families.Length != 1 || selectedPaths.Any(path =>
            !string.Equals(Path.GetExtension(path), ".rfa", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Path.GetExtension(path), ".ifc", StringComparison.OrdinalIgnoreCase)))
        {
            SetError("Выберите один RFA-файл и, при необходимости, вспомогательные CSV или IFC.");
            return;
        }

        var path = families[0];
        var auxiliaryPaths = selectedPaths.Where(filePath =>
            !string.Equals(Path.GetExtension(filePath), ".rfa", StringComparison.OrdinalIgnoreCase)).ToArray();
        _session?.Dispose();
        _session = null;
        FamilyMetadata.Family = null;
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
        AuxiliaryFileNames = "Вспомогательные файлы не выбраны";
        IsLoading = true;
        ErrorMessage = null;
        Message = $"Читаем семейство через коннектор «{connector.Name}»…";
        try
        {
            var categories = await _api.Categories.GetAllAsync();
            Categories.Clear();
            foreach (var category in categories.OrderBy(category => category.Name))
            {
                Categories.Add(category);
            }

            _session = await _import.ReadAsync(path, connector, auxiliaryPaths);
            AuxiliaryFileNames = _session.AuxiliaryFiles.Count == 0
                ? "Вспомогательные файлы не выбраны"
                : string.Join(Environment.NewLine, _session.AuxiliaryFiles.Select(file => file.Name));
            ContentName = _session.ContentName;
            ConnectorName = _session.Connector.Name;
            FamilyCategory = _session.Family.RevitCategory ?? string.Empty;
            var year = Regex.Match(_session.Family.ApplicationVersion ?? _session.Connector.ProductVersion, @"20\d{2}");
            ApplicationVersion = year.Success ? year.Value : string.Empty;
            MetadataJson = _session.Metadata.ToString(Formatting.Indented);
            FamilyMetadata.Family = _session.Family;

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
            FamilyMetadata.Family = null;
            SetError($"Не удалось подготовить семейство: {exception.Message}");
        }
        finally
        {
            IsLoading = false;
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

        IsSaving = true;
        ErrorMessage = null;
        Message = "Сохраняем новую версию, RFA, вспомогательные файлы и метаданные…";
        try
        {
            SavedVersion = await _import.SaveAsync(
                _session, Description, SelectedCategory?.Id, VersionName, ApplicationVersion);
            Message = "Новая версия, файлы и метаданные сохранены.";
            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException or OperationCanceledException)
        {
            SetError($"Не удалось сохранить версию: {exception.Message}");
            return false;
        }
        finally
        {
            IsSaving = false;
            OnPropertyChanged(nameof(CanEditContent));
            OnPropertyChanged(nameof(CanEditVersion));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _import.ConnectorsChanged -= OnConnectorsChanged;
        _session?.Dispose();
        _session = null;
    }

    protected override void OnBusyStateChanged()
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanChangeConnector));
        OnPropertyChanged(nameof(CanChooseFile));
    }

    private void OnConnectorsChanged(object? sender, EventArgs eventArgs)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            UpdateConnectors();
        }
        else
        {
            Dispatcher.UIThread.Post(UpdateConnectors);
        }
    }

    private void UpdateConnectors()
    {
        if (_disposed)
        {
            return;
        }

        var selectedId = SelectedConnector?.InstanceId;
        var connectors = _import.GetAvailableConnectors();
        Connectors.Clear();
        foreach (var connector in connectors)
        {
            Connectors.Add(connector);
        }

        SelectedConnector = Connectors.FirstOrDefault(connector => connector.InstanceId == selectedId)
            ?? Connectors.FirstOrDefault();
        OnPropertyChanged(nameof(CanChooseFile));
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        if (await SaveAsync() && SavedVersion is not null)
        {
            Complete(SavedVersion);
        }
    }
}
