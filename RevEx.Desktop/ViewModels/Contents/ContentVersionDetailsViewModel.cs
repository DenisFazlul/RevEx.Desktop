using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentVersionDetailsViewModel : WorkspaceTabViewModel
{
    private readonly IRevExApiService _apiService;

    [ObservableProperty] private ContentVersionDto _version;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<ContentVersionFileItemViewModel> Files { get; } = [];
    public bool HasFiles => Files.Count > 0;
    public bool HasNoFiles => !HasFiles && !IsLoading;
    public string DateText => Version.Date.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    public string CreatedAtText => Version.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    public string UpdatedAtText => Version.UpdatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    public ContentVersionDetailsViewModel(ContentVersionDto version, IRevExApiService apiService)
        : base($"Версия: {version.Name}", true, version.Id)
    {
        _version = version;
        _apiService = apiService;
    }

    public override async Task ActivateAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var versionTask = _apiService.GetContentVersionAsync(Version.Id);
            var filesTask = _apiService.GetContentFilesAsync();
            await Task.WhenAll(versionTask, filesTask);

            Version = await versionTask
                ?? throw new HttpRequestException($"Версия {Version.Id} не найдена.");
            Title = $"Версия: {Version.Name}";

            Files.Clear();
            foreach (var file in (await filesTask)
                         .Where(item => item.ContentVersionId == Version.Id)
                         .OrderBy(item => item.Role)
                         .ThenBy(item => item.OriginalFileName))
                Files.Add(new ContentVersionFileItemViewModel(file));

            NotifyStateChanged();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось загрузить версию: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    partial void OnVersionChanged(ContentVersionDto value)
    {
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(CreatedAtText));
        OnPropertyChanged(nameof(UpdatedAtText));
    }

    partial void OnIsLoadingChanged(bool value) => NotifyStateChanged();

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasNoFiles));
    }
}

public sealed class ContentVersionFileItemViewModel
{
    public int Id { get; }
    public string Name { get; }
    public string Extension { get; }
    public string Role { get; }
    public string SizeText { get; }

    public ContentVersionFileItemViewModel(ContentFileDto file)
    {
        Id = file.Id;
        Name = file.OriginalFileName;
        Extension = file.Extension;
        Role = file.Role;
        SizeText = FormatSize(file.SizeBytes);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} Б",
        < 1024 * 1024 => $"{bytes / 1024d:0.#} КБ",
        _ => $"{bytes / (1024d * 1024d):0.#} МБ"
    };
}
