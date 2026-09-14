using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Tags;

public partial class TagAdminTabViewModel(IRevExApiService apiService)
    : WorkspaceTabViewModel("Теги", true, "tags")
{
    private int? _pendingDeletionId;

    [ObservableProperty] private TagItemViewModel? _selectedTag;
    [ObservableProperty] private string _tagName = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _message;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _deleteButtonText = "Удалить";

    public ObservableCollection<TagItemViewModel> Tags { get; } = [];
    public bool HasTags => Tags.Count > 0;
    public bool HasNoTags => !HasTags && !IsLoading;

    public override async Task ActivateAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var tags = await apiService.GetTagsAsync();
            Tags.Clear();
            foreach (var tag in tags.OrderBy(item => item.Name))
                Tags.Add(new TagItemViewModel(tag));
            NotifyTagStateChanged();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось загрузить теги: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyTagStateChanged();
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var name = TagName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Укажите название тега.";
            return;
        }

        await RunAsync(async () =>
        {
            var created = await apiService.CreateTagAsync(new CreateTagDto { Name = name });
            var item = new TagItemViewModel(created);
            Tags.Add(item);
            SelectedTag = item;
            Message = "Тег создан.";
            NotifyTagStateChanged();
        });
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        if (SelectedTag is null)
        {
            ErrorMessage = "Выберите тег для переименования.";
            return;
        }

        var name = TagName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Укажите новое название тега.";
            return;
        }

        await RunAsync(async () =>
        {
            await apiService.UpdateTagAsync(SelectedTag.Id, new UpdateTagDto { Name = name });
            SelectedTag.Name = name;
            Message = "Тег переименован.";
        });
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedTag is null)
        {
            ErrorMessage = "Выберите тег для удаления.";
            return;
        }

        if (_pendingDeletionId != SelectedTag.Id)
        {
            _pendingDeletionId = SelectedTag.Id;
            DeleteButtonText = "Подтвердить удаление";
            Message = $"Тег «{SelectedTag.Name}» будет снят со всех материалов. Нажмите ещё раз для подтверждения.";
            return;
        }

        await RunAsync(async () =>
        {
            var tag = SelectedTag;
            await apiService.DeleteTagAsync(tag.Id);
            Tags.Remove(tag);
            SelectedTag = null;
            TagName = string.Empty;
            Message = "Тег удалён.";
            NotifyTagStateChanged();
        });
    }

    partial void OnSelectedTagChanged(TagItemViewModel? value)
    {
        TagName = value?.Name ?? string.Empty;
        ResetDeleteConfirmation();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyTagStateChanged();

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;
            Message = null;
            await action();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось изменить теги: {exception.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ResetDeleteConfirmation()
    {
        _pendingDeletionId = null;
        DeleteButtonText = "Удалить";
    }

    private void NotifyTagStateChanged()
    {
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(HasNoTags));
    }
}
