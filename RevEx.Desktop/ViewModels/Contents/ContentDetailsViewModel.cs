using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentDetailsViewModel : Tabs.WorkspaceTabViewModel
{
    private readonly Action _openVersionEditor;
    private readonly IRevExApiService _apiService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public int Id { get; }
    public string Description { get; }
    public ObservableCollection<ContentVersionDto> Versions { get; } = [];
    public bool HasVersions => Versions.Count > 0;
    public bool HasNoVersions => !HasVersions && !IsLoading;

    public ContentDetailsViewModel(
        ContentItemViewModel content,
        IRevExApiService apiService,
        Action openVersionEditor)
        : base(content.Name, true, content.Id)
    {
        _apiService = apiService;
        _openVersionEditor = openVersionEditor;
        Id = content.Id;
        Description = content.Description;
    }

    public override async Task ActivateAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var versions = await _apiService.GetContentVersionsAsync();

            Versions.Clear();
            foreach (var version in versions
                         .Where(item => item.ContentId == Id)
                         .OrderByDescending(item => item.Date))
            {
                Versions.Add(version);
            }

            NotifyVersionStateChanged();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось загрузить версии: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyVersionStateChanged();
        }
    }

    public void AddOrUpdateVersion(ContentVersionDto version)
    {
        var existing = Versions.FirstOrDefault(item => item.Id == version.Id);
        if (existing is not null)
            Versions.Remove(existing);

        Versions.Insert(0, version);
        NotifyVersionStateChanged();
    }

    [RelayCommand]
    private void AddVersion() => _openVersionEditor();

    partial void OnIsLoadingChanged(bool value) => NotifyVersionStateChanged();

    private void NotifyVersionStateChanged()
    {
        OnPropertyChanged(nameof(HasVersions));
        OnPropertyChanged(nameof(HasNoVersions));
    }
}
