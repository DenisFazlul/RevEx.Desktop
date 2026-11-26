using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.ContentSet;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentSetsViewModel : DialogViewModelBase
{
    private readonly IContentSetRepository _sets;
    private readonly int _contentId;
    private IReadOnlyCollection<ContentSetDto> _all = [];
    private readonly Func<string?> _currentAuthor;

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private ContentSetDto? _selectedSet;

    public ObservableCollection<ContentSetDto> Sets { get; } = [];

    public ContentSetsViewModel(IContentSetRepository sets, int contentId, Func<string?>? currentAuthor = null)
    {
        _sets = sets;
        _currentAuthor = currentAuthor ?? (() => null);
        _contentId = contentId;
    }

    public Task InitializeAsync()
    {
        return RunLoadAsync(async () =>
        {
            _all = await _sets.GetAllAsync();
            Filter();
        }, "Не удалось загрузить наборы");
    }

    partial void OnSearchChanged(string value)
    {
        Filter();
    }

    private void Filter()
    {
        var selectedId = SelectedSet?.Id;
        var filtered = ContentSetList.FilterAndSort(_all, Search, _currentAuthor());
        Sets.Clear();
        foreach (var item in filtered)
        {
            Sets.Add(item);
        }

        SelectedSet = filtered.FirstOrDefault(item => item.Id == selectedId);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (SelectedSet is not { } selected)
        {
            SetError("Выберите набор.");
            return;
        }

        var saved = await RunSaveAsync(
            () => _sets.AddContentAsync(selected.Id, _contentId),
            "Не удалось добавить контент");
        if (saved)
        {
            RequestClose(null);
        }
    }
}
