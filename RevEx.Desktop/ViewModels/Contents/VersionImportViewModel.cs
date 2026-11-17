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
using Newtonsoft.Json;
using RevEx.Desktop.Services.VersionImport;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.ViewModels.Revit;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class VersionImportViewModel : DialogViewModel<ContentVersionDto>, IDisposable
{
    private readonly ContentDto _content;
    private readonly VersionFileConnectorCatalog _catalog;
    private readonly VersionImportService _import;
    private readonly List<string> _paths = [];
    private VersionImportSession? _session;
    private bool _disposed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReadFiles))]
    private VersionPlatformOption _selectedPlatform;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReadFiles))]
    private IVersionFileConnector? _selectedConnector;

    [ObservableProperty]
    private string _selectedFileNames = "Файлы не выбраны";

    [ObservableProperty]
    private string _versionName = string.Empty;

    [ObservableProperty]
    private string _applicationVersion = string.Empty;

    public VersionImportViewModel(
        ContentDto content,
        Platform initialPlatform,
        VersionFileConnectorCatalog catalog,
        VersionImportService import)
    {
        _content = content;
        _catalog = catalog;
        _import = import;
        _selectedPlatform = Platforms.First(option => option.Value == initialPlatform);
        _catalog.Changed += OnConnectorsChanged;
        UpdateConnectors();
    }

    public IReadOnlyList<VersionPlatformOption> Platforms { get; } =
    [
        new(Platform.Revit, "Revit"),
        new(Platform.AutoCad, "AutoCAD"),
        new(Platform.Other, "Другое")
    ];

    public ObservableCollection<IVersionFileConnector> Connectors { get; } = [];

    public ObservableCollection<VersionImportFileViewModel> MetadataFiles { get; } = [];

    public RevitFamilyReadResultViewModel RevitMetadata { get; } = new();

    public bool IsSelectionStep => _session is null;

    public bool IsReviewStep => _session is not null;

    public bool HasRevitMetadata => RevitMetadata.HasFamily;

    public bool HasGenericMetadata => IsReviewStep && !HasRevitMetadata;

    public bool CanChooseFiles => IsSelectionStep && !IsBusy;

    public bool CanReadFiles => CanChooseFiles && SelectedConnector is not null && _paths.Count > 0;

    public bool CanSave => IsReviewStep && !IsBusy;

    public void SetFiles(IReadOnlyCollection<string> paths)
    {
        if (!CanChooseFiles)
        {
            return;
        }

        _paths.Clear();
        _paths.AddRange(paths.Distinct(StringComparer.OrdinalIgnoreCase));
        SelectedFileNames = _paths.Count == 0
            ? "Файлы не выбраны"
            : string.Join(Environment.NewLine, _paths.Select(Path.GetFileName));
        ClearMessage();
        NotifyStateChanged();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _catalog.Changed -= OnConnectorsChanged;
        _session?.Dispose();
        _session = null;
    }

    partial void OnSelectedPlatformChanged(VersionPlatformOption value)
    {
        UpdateConnectors();
    }

    partial void OnSelectedConnectorChanged(IVersionFileConnector? value)
    {
        NotifyStateChanged();
    }

    protected override void OnBusyStateChanged()
    {
        NotifyStateChanged();
    }

    [RelayCommand]
    private async Task ReadFilesAsync()
    {
        if (!CanReadFiles || SelectedConnector is null)
        {
            return;
        }

        IsLoading = true;
        ClearMessage();
        try
        {
            _session = await _import.ReadAsync(
                _content,
                SelectedPlatform.Value,
                SelectedConnector,
                _paths);
            VersionName = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
            ApplicationVersion = NormalizeApplicationVersion(_session.SuggestedApplicationVersion);
            RevitMetadata.Family = _session.RevitFamily;
            MetadataFiles.Clear();
            foreach (var file in _session.Files)
            {
                MetadataFiles.Add(new VersionImportFileViewModel(file));
            }

            Message = "Метаданные прочитаны. Проверьте данные и добавьте версию.";
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or
                                             InvalidOperationException or UnauthorizedAccessException or
                                             OperationCanceledException or JsonException)
        {
            _session?.Dispose();
            _session = null;
            SetError($"Не удалось прочитать файлы: {exception.Message}");
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (!IsReviewStep || IsBusy)
        {
            return;
        }

        _session?.Dispose();
        _session = null;
        RevitMetadata.Family = null;
        MetadataFiles.Clear();
        ClearMessage();
        NotifyStateChanged();
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        if (!CanSave || _session is null)
        {
            return;
        }

        if (_session.Platform == Platform.Revit && !Regex.IsMatch(ApplicationVersion, @"^20\d{2}$"))
        {
            SetError("Для Revit укажите год приложения четырьмя цифрами.");
            return;
        }

        IsSaving = true;
        ClearMessage();
        try
        {
            var result = await _import.SaveAsync(_session, VersionName, ApplicationVersion);
            Complete(result);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or
                                             InvalidOperationException or UnauthorizedAccessException or
                                             OperationCanceledException)
        {
            SetError($"Не удалось добавить версию: {exception.Message}");
        }
        finally
        {
            IsSaving = false;
            NotifyStateChanged();
        }
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
        if (_disposed || !IsSelectionStep)
        {
            return;
        }

        var selectedId = SelectedConnector?.Id;
        var connectors = _catalog.GetAll(SelectedPlatform.Value);
        Connectors.Clear();
        foreach (var connector in connectors)
        {
            Connectors.Add(connector);
        }

        SelectedConnector = Connectors.FirstOrDefault(connector => connector.Id == selectedId)
            ?? Connectors.FirstOrDefault();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(IsSelectionStep));
        OnPropertyChanged(nameof(IsReviewStep));
        OnPropertyChanged(nameof(HasRevitMetadata));
        OnPropertyChanged(nameof(HasGenericMetadata));
        OnPropertyChanged(nameof(CanChooseFiles));
        OnPropertyChanged(nameof(CanReadFiles));
        OnPropertyChanged(nameof(CanSave));
        ReadFilesCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
        SaveAndCloseCommand.NotifyCanExecuteChanged();
    }

    private static string NormalizeApplicationVersion(string value)
    {
        var year = Regex.Match(value, @"20\d{2}");
        return year.Success ? year.Value : value;
    }
}
