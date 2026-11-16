using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Создаёт версию платформы Other из свойств выбранного локального файла.</summary>
public partial class FileVersionImportViewModel : DialogViewModel<ContentVersionDto>
{
    private const long MaximumFileSizeBytes = 100 * 1024 * 1024;
    private readonly IRevExApiService _api;
    private readonly int _contentId;
    private string? _filePath;
    private DateTimeOffset _fileCreationTime;
    private DateTimeOffset _fileLastWriteTime;
    private long _fileSizeBytes;
    private FileAttributes _fileAttributes;
    private ContentVersionDto? _createdVersion;

    [ObservableProperty]
    private string _fileName = "Файл не выбран";

    [ObservableProperty]
    private string _extension = string.Empty;

    [ObservableProperty]
    private string _sizeText = string.Empty;

    [ObservableProperty]
    private string _creationTimeText = string.Empty;

    [ObservableProperty]
    private string _lastWriteTimeText = string.Empty;

    public FileVersionImportViewModel(IRevExApiService api, int contentId)
    {
        _api = api;
        _contentId = contentId;
    }

    public bool CanChooseFile => !IsBusy && _createdVersion is null;
    public bool CanSave => !IsBusy && _filePath is not null;

    public void LoadFile(string path)
    {
        if (!CanChooseFile)
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        var file = new FileInfo(fullPath);
        if (!file.Exists)
        {
            SetError("Выбранный файл не существует.");
            return;
        }

        if (file.Length == 0 || file.Length > MaximumFileSizeBytes)
        {
            SetError("Размер файла должен быть от 1 байта до 100 МБ.");
            return;
        }

        _filePath = fullPath;
        _fileSizeBytes = file.Length;
        _fileCreationTime = new DateTimeOffset(file.CreationTimeUtc, TimeSpan.Zero);
        _fileLastWriteTime = new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero);
        _fileAttributes = file.Attributes;
        FileName = file.Name;
        Extension = string.IsNullOrWhiteSpace(file.Extension) ? "Без расширения" : file.Extension.ToLowerInvariant();
        SizeText = FormatSize(file.Length);
        CreationTimeText = _fileCreationTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
        LastWriteTimeText = _fileLastWriteTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
        ClearMessage();
        Message = "Файл готов к добавлению.";
        NotifyStateChanged();
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        if (!CanSave || _filePath is null)
        {
            return;
        }

        IsSaving = true;
        ClearMessage();
        try
        {
            _createdVersion ??= await _api.ContentVersions.CreateAsync(new CreateContentVersionDto
            {
                ContentId = _contentId,
                Name = FileName,
                Date = _fileCreationTime,
                PublishStatus = PublishStatus.Draft,
                Platform = Platform.Other,
                Application = "file",
                ApplicationVersion = GetApplicationVersion()
            });

            var metadata = CreateMetadata();
            await using var stream = File.OpenRead(_filePath);
            var result = await _api.ContentVersions.UploadFileAsync(
                _createdVersion.Id,
                stream,
                FileName,
                "primary",
                metadataJson: metadata.ToString(Newtonsoft.Json.Formatting.None));
            Complete(result);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or
                                             UnauthorizedAccessException or OperationCanceledException or
                                             InvalidOperationException)
        {
            SetError($"Не удалось добавить версию: {exception.Message}");
        }
        finally
        {
            IsSaving = false;
            NotifyStateChanged();
        }
    }

    protected override void OnBusyStateChanged()
    {
        NotifyStateChanged();
    }

    private JObject CreateMetadata()
    {
        return new JObject
        {
            ["FileName"] = FileName,
            ["Extension"] = Path.GetExtension(FileName).ToLowerInvariant(),
            ["SizeBytes"] = _fileSizeBytes,
            ["CreationTimeUtc"] = _fileCreationTime.UtcDateTime,
            ["LastWriteTimeUtc"] = _fileLastWriteTime.UtcDateTime,
            ["Attributes"] = _fileAttributes.ToString()
        };
    }

    private string GetApplicationVersion()
    {
        var extension = Path.GetExtension(FileName).TrimStart('.').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(extension) ? "file" : extension;
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CanChooseFile));
        OnPropertyChanged(nameof(CanSave));
        SaveAndCloseCommand.NotifyCanExecuteChanged();
    }

    private static string FormatSize(long sizeBytes)
    {
        if (sizeBytes < 1024)
        {
            return $"{sizeBytes} Б";
        }

        if (sizeBytes < 1024 * 1024)
        {
            return $"{sizeBytes / 1024d:0.##} КБ";
        }

        return $"{sizeBytes / (1024d * 1024d):0.##} МБ";
    }
}
