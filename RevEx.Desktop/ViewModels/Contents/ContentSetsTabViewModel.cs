using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Desktop.ViewModels.Tabs;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentSet;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentSetsTabViewModel : WorkspaceTabViewModel
{
    private readonly IRevExApiService _api;
    private readonly Action<ContentItemViewModel> _openContent;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _contentLoad;
    private IReadOnlyCollection<ContentSetDto> _all = [];
    private readonly Func<string?> _currentAuthor;
    private bool _disposed;

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private ContentSetDto? _selectedSet;

    [ObservableProperty]
    private ContentItemViewModel? _selectedContent;

    [ObservableProperty]
    private bool _isContentLoading;

    public ObservableCollection<ContentSetDto> Sets { get; } = [];
    public ObservableCollection<ContentItemViewModel> Contents { get; } = [];
    public bool HasSelectedSet => SelectedSet is not null;
    public bool HasSelectedContent => SelectedContent is not null;

    public ContentSetsTabViewModel(IRevExApiService api, Action<ContentItemViewModel> openContent, Func<string?>? currentAuthor = null)
        : base("Наборы", true)
    {
        _api = api;
        _currentAuthor = currentAuthor ?? (() => null);
        _openContent = openContent;
    }

    public override Task ActivateAsync()
    {
        return RefreshAsync(SelectedSet?.Id);
    }

    public Task RefreshAsync(int? selectedId = null)
    {
        return RunLoadAsync(async () =>
        {
            _all = await _api.ContentSets.GetAllAsync(_lifetime.Token);
            if (_disposed)
            {
                return;
            }

            var previous = SelectedSet;
            Filter(selectedId ?? SelectedSet?.Id);
            if (SelectedSet is { } selected && ReferenceEquals(previous, SelectedSet))
            {
                _contentLoad?.Cancel();
                await LoadContentsAsync(selected.Id);
            }
        }, "Не удалось загрузить наборы");
    }

    partial void OnSearchChanged(string value)
    {
        Filter(SelectedSet?.Id);
    }

    private void Filter(int? selectedId)
    {
        var filtered = ContentSetList.FilterAndSort(_all, Search, _currentAuthor());
        Sets.Clear();
        foreach (var item in filtered)
        {
            Sets.Add(item);
        }

        SelectedSet = filtered.FirstOrDefault(item => item.Id == selectedId);
    }

    partial void OnSelectedSetChanged(ContentSetDto? value)
    {
        OnPropertyChanged(nameof(HasSelectedSet));
        _contentLoad?.Cancel();
        ClearContents();
        if (value is not null)
        {
            _ = LoadContentsAsync(value.Id);
        }
        else
        {
            IsContentLoading = false;
        }
    }

    partial void OnSelectedContentChanged(ContentItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedContent));
    }

    private async Task LoadContentsAsync(int id)
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _contentLoad = cancellation;
        IsContentLoading = true;
        var loaded = new List<ContentItemViewModel>();
        try
        {
            var membersTask = _api.ContentSets.GetContentsAsync(id, cancellation.Token);
            var tagsTask = _api.Tags.GetAllAsync(cancellation.Token);
            await Task.WhenAll(membersTask, tagsTask);
            var tags = (await tagsTask).ToDictionary(item => item.Id, item => new TagFilterItemViewModel(item));
            foreach (var member in await membersTask)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var content = await _api.Contents.GetByIdAsync(member.Id, cancellation.Token);
                if (content is null)
                {
                    continue;
                }

                byte[]? preview = null;
                if (content.PreviewFileId is int previewId)
                {
                    try
                    {
                        preview = await _api.ContentFiles.DownloadAsync(previewId, cancellation.Token);
                    }
                    catch (HttpRequestException)
                    {
                        // Как в каталоге, недоступное превью не скрывает сам контент.
                    }
                }

                loaded.Add(new ContentItemViewModel(content, tags, preview));
            }

            if (_disposed || cancellation.IsCancellationRequested || !ReferenceEquals(_contentLoad, cancellation))
            {
                return;
            }

            ClearContents();
            foreach (var item in loaded)
            {
                Contents.Add(item);
            }

            loaded.Clear();
        }
        catch (Exception exception) when (IsExpectedError(exception))
        {
            if (!_disposed && !cancellation.IsCancellationRequested && ReferenceEquals(_contentLoad, cancellation))
            {
                SetError($"Не удалось загрузить состав набора: {exception.Message}");
            }
        }
        finally
        {
            foreach (var item in loaded)
            {
                item.Dispose();
            }

            if (ReferenceEquals(_contentLoad, cancellation))
            {
                _contentLoad = null;
                IsContentLoading = false;
            }

            cancellation.Dispose();
        }
    }

    public ContentSetEditorViewModel CreateEditor(bool isNew)
    {
        return new ContentSetEditorViewModel(_api.ContentSets, isNew ? null : SelectedSet);
    }

    public void OpenContent(ContentItemViewModel content)
    {
        _openContent(content);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedSet is not { } set)
        {
            return;
        }

        await RunSaveAsync(async () =>
        {
            await _api.ContentSets.DeleteAsync(set.Id, _lifetime.Token);
            SelectedSet = null;
            await RefreshAsync();
            Message = "Набор удалён. Контент сохранён.";
        }, "Не удалось удалить набор");
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        if (SelectedSet is not { } set || SelectedContent is not { } content)
        {
            return;
        }

        await RunSaveAsync(async () =>
        {
            await _api.ContentSets.RemoveContentAsync(set.Id, content.Id, _lifetime.Token);
            Contents.Remove(content);
            SelectedContent = null;
            content.Dispose();
        }, "Не удалось убрать контент из набора");
    }

    private void ClearContents()
    {
        SelectedContent = null;
        foreach (var item in Contents)
        {
            item.Dispose();
        }

        Contents.Clear();
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
        ClearContents();
        base.Dispose();
    }
}
