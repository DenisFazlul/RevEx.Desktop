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
    : WorkspaceTabViewModel("Группы тегов", true, "tags")
{
    private int? _pendingGroupDeletionId;
    private int? _pendingTagDeletionId;

    [ObservableProperty] private TagGroupItemViewModel? _selectedGroup;
    [ObservableProperty] private TagItemViewModel? _selectedTag;
    [ObservableProperty] private TagGroupItemViewModel? _selectedTagGroup;
    [ObservableProperty] private string _groupName = string.Empty;
    [ObservableProperty] private string _tagName = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _message;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _deleteGroupButtonText = "Удалить группу";
    [ObservableProperty] private string _deleteTagButtonText = "Удалить тег";
    [ObservableProperty] private bool _isGroupEditorOpen;

    public ObservableCollection<TagGroupItemViewModel> Groups { get; } = [];
    public bool HasGroups => Groups.Count > 0;
    public bool HasNoGroups => !HasGroups && !IsLoading;
    public bool HasSelectedGroup => SelectedGroup is not null;
    public bool HasTags => SelectedGroup?.Tags.Count > 0;
    public bool HasNoTags => HasSelectedGroup && !HasTags && !IsLoading;
    public bool HasSelectedTag => SelectedTag is not null;

    public override async Task ActivateAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var groupsTask = apiService.GetTagGroupsAsync();
            var tagsTask = apiService.GetTagsAsync();
            await Task.WhenAll(groupsTask, tagsTask);
            Groups.Clear();
            foreach (var groupDto in (await groupsTask).OrderBy(item => item.Name))
            {
                var group = CreateGroupItem(groupDto);
                foreach (var tag in (await tagsTask).Where(item => item.TagGroupId == group.Id).OrderBy(item => item.Name))
                    group.Tags.Add(new TagItemViewModel(tag));
                Groups.Add(group);
            }
            SelectedGroup = null;
            SelectedTagGroup = Groups.FirstOrDefault();
            NotifyStateChanged();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { ErrorMessage = $"Не удалось загрузить группы тегов: {exception.Message}"; }
        finally { IsLoading = false; NotifyStateChanged(); }
    }

    [RelayCommand]
    private void BeginCreateGroup()
    {
        SelectedGroup = null;
        GroupName = string.Empty;
        IsGroupEditorOpen = true;
        ResetConfirmations();
    }

    [RelayCommand]
    private async Task SaveGroupAsync()
    {
        var name = GroupName.Trim();
        if (string.IsNullOrWhiteSpace(name)) { ErrorMessage = "Укажите название группы."; return; }
        await RunAsync(async () =>
        {
            if (SelectedGroup is null)
            {
                var created = await apiService.CreateTagGroupAsync(new CreateTagGroupDto { Name = name });
                Groups.Add(CreateGroupItem(created));
                Message = "Группа создана.";
            }
            else
            {
                await apiService.UpdateTagGroupAsync(SelectedGroup.Id, new UpdateTagGroupDto { Name = name });
                SelectedGroup.Name = name;
                Message = "Группа переименована.";
            }
            IsGroupEditorOpen = false;
            NotifyStateChanged();
        });
    }

    [RelayCommand]
    private void CancelGroupEdit()
    {
        IsGroupEditorOpen = false;
        SelectedGroup = null;
        GroupName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        if (SelectedGroup is null) { ErrorMessage = "Выберите группу."; return; }
        if (SelectedGroup.Tags.Count > 0) { ErrorMessage = "Сначала удалите все теги группы."; return; }
        if (_pendingGroupDeletionId != SelectedGroup.Id)
        {
            _pendingGroupDeletionId = SelectedGroup.Id;
            DeleteGroupButtonText = "Подтвердить удаление";
            Message = $"Группа «{SelectedGroup.Name}» будет удалена. Нажмите ещё раз для подтверждения.";
            return;
        }
        await RunAsync(async () =>
        {
            var group = SelectedGroup;
            await apiService.DeleteTagGroupAsync(group.Id);
            Groups.Remove(group);
            SelectedGroup = null;
            IsGroupEditorOpen = false;
            Message = "Группа удалена.";
            NotifyStateChanged();
        });
    }

    [RelayCommand]
    private async Task CreateTagAsync()
    {
        if (SelectedTagGroup is null) { ErrorMessage = "Сначала выберите группу."; return; }
        var name = TagName.Trim();
        if (string.IsNullOrWhiteSpace(name)) { ErrorMessage = "Укажите название тега."; return; }
        await RunAsync(async () =>
        {
            var created = await apiService.CreateTagAsync(new CreateTagDto { Name = name, TagGroupId = SelectedTagGroup.Id });
            var tag = new TagItemViewModel(created);
            SelectedTagGroup.Tags.Add(tag);
            SelectedTagGroup.NotifyTagCountChanged();
            SelectedTagGroup.SelectedTag = tag;
            Message = "Тег создан в выбранной группе.";
            NotifyStateChanged();
        });
    }

    [RelayCommand]
    private async Task RenameTagAsync()
    {
        if (SelectedTag is null || SelectedTagGroup is null) { ErrorMessage = "Выберите тег и группу."; return; }
        var name = TagName.Trim();
        if (string.IsNullOrWhiteSpace(name)) { ErrorMessage = "Укажите название тега."; return; }
        await RunAsync(async () =>
        {
            var tag = SelectedTag;
            var targetGroup = SelectedTagGroup;
            var oldGroup = Groups.First(group => group.Id == tag.TagGroupId);
            await apiService.UpdateTagAsync(tag.Id, new UpdateTagDto { Name = name, TagGroupId = targetGroup.Id });
            tag.Name = name;
            if (oldGroup.Id != targetGroup.Id)
            {
                oldGroup.Tags.Remove(tag);
                oldGroup.SelectedTag = null;
                oldGroup.NotifyTagCountChanged();
                tag.TagGroupId = targetGroup.Id;
                targetGroup.Tags.Add(tag);
                targetGroup.SelectedTag = tag;
                targetGroup.NotifyTagCountChanged();
            }
            Message = "Тег сохранён.";
        });
    }

    [RelayCommand]
    private async Task DeleteTagAsync()
    {
        if (SelectedTag is null) { ErrorMessage = "Выберите тег."; return; }
        if (_pendingTagDeletionId != SelectedTag.Id)
        {
            _pendingTagDeletionId = SelectedTag.Id;
            DeleteTagButtonText = "Подтвердить удаление";
            Message = $"Тег «{SelectedTag.Name}» будет снят со всех материалов. Нажмите ещё раз для подтверждения.";
            return;
        }
        await RunAsync(async () =>
        {
            var tag = SelectedTag;
            await apiService.DeleteTagAsync(tag.Id);
            var group = Groups.First(item => item.Id == tag.TagGroupId);
            group.Tags.Remove(tag);
            group.SelectedTag = null;
            group.NotifyTagCountChanged();
            SelectedTag = null;
            Message = "Тег удалён.";
            NotifyStateChanged();
        });
    }

    partial void OnSelectedGroupChanged(TagGroupItemViewModel? value)
    {
        GroupName = value?.Name ?? string.Empty;
        ResetConfirmations();
        NotifyStateChanged();
    }

    partial void OnSelectedTagChanged(TagItemViewModel? value)
    {
        TagName = value?.Name ?? string.Empty;
        SelectedTagGroup = value is null ? null : Groups.FirstOrDefault(group => group.Id == value.TagGroupId);
        _pendingTagDeletionId = null;
        DeleteTagButtonText = "Удалить тег";
        NotifyStateChanged();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyStateChanged();

    private async Task RunAsync(Func<Task> action)
    {
        try { IsSaving = true; ErrorMessage = null; Message = null; await action(); }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { ErrorMessage = $"Не удалось изменить группы тегов: {exception.Message}"; }
        finally { IsSaving = false; }
    }

    private void ResetConfirmations()
    {
        _pendingGroupDeletionId = null;
        _pendingTagDeletionId = null;
        DeleteGroupButtonText = "Удалить группу";
        DeleteTagButtonText = "Удалить тег";
    }

    private TagGroupItemViewModel CreateGroupItem(TagGroupDto dto) =>
        new(dto, BeginEditGroup, SelectTag);

    private void BeginEditGroup(TagGroupItemViewModel group)
    {
        SelectedGroup = group;
        GroupName = group.Name;
        IsGroupEditorOpen = true;
        ResetConfirmations();
    }

    private void SelectTag(TagGroupItemViewModel group, TagItemViewModel? tag)
    {
        if (tag is null && SelectedTag?.TagGroupId != group.Id) return;
        foreach (var otherGroup in Groups.Where(item => item != group && item.SelectedTag is not null))
            otherGroup.SelectedTag = null;
        SelectedTag = tag;
        if (tag is not null)
            SelectedTagGroup = group;
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasGroups));
        OnPropertyChanged(nameof(HasNoGroups));
        OnPropertyChanged(nameof(HasSelectedGroup));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(HasNoTags));
        OnPropertyChanged(nameof(HasSelectedTag));
    }

    public async Task<bool> AddGroupFromDialogAsync(string name) => await ExecuteDialogActionAsync(async () =>
    {
        var created = await apiService.CreateTagGroupAsync(new CreateTagGroupDto { Name = name.Trim() });
        Groups.Add(CreateGroupItem(created));
        Message = "Группа создана.";
        NotifyStateChanged();
    });

    public async Task<bool> EditGroupFromDialogAsync(TagGroupItemViewModel group, string name) =>
        await ExecuteDialogActionAsync(async () =>
        {
            await apiService.UpdateTagGroupAsync(group.Id, new UpdateTagGroupDto { Name = name.Trim() });
            group.Name = name.Trim();
            Message = "Группа переименована.";
        });

    public async Task<bool> DeleteGroupFromDialogAsync(TagGroupItemViewModel group) =>
        await ExecuteDialogActionAsync(async () =>
        {
            if (group.Tags.Count > 0) throw new InvalidOperationException("Сначала удалите или перенесите теги группы.");
            await apiService.DeleteTagGroupAsync(group.Id);
            Groups.Remove(group);
            Message = "Группа удалена.";
            NotifyStateChanged();
        });

    public async Task<bool> AddTagFromDialogAsync(string name, TagGroupItemViewModel group) =>
        await ExecuteDialogActionAsync(async () =>
        {
            var created = await apiService.CreateTagAsync(new CreateTagDto { Name = name.Trim(), TagGroupId = group.Id });
            group.Tags.Add(new TagItemViewModel(created));
            group.NotifyTagCountChanged();
            Message = "Тег создан.";
        });

    public async Task<bool> EditTagFromDialogAsync(
        TagItemViewModel tag,
        string name,
        TagGroupItemViewModel targetGroup) => await ExecuteDialogActionAsync(async () =>
    {
        var oldGroup = Groups.First(group => group.Id == tag.TagGroupId);
        await apiService.UpdateTagAsync(tag.Id, new UpdateTagDto { Name = name.Trim(), TagGroupId = targetGroup.Id });
        tag.Name = name.Trim();
        if (oldGroup.Id != targetGroup.Id)
        {
            oldGroup.Tags.Remove(tag);
            oldGroup.NotifyTagCountChanged();
            tag.TagGroupId = targetGroup.Id;
            targetGroup.Tags.Add(tag);
            targetGroup.NotifyTagCountChanged();
        }
        Message = "Тег сохранён.";
    });

    public async Task<bool> DeleteTagFromDialogAsync(TagItemViewModel tag) =>
        await ExecuteDialogActionAsync(async () =>
        {
            await apiService.DeleteTagAsync(tag.Id);
            var group = Groups.First(item => item.Id == tag.TagGroupId);
            group.Tags.Remove(tag);
            group.NotifyTagCountChanged();
            Message = "Тег удалён.";
        });

    private async Task<bool> ExecuteDialogActionAsync(Func<Task> action)
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;
            Message = null;
            await action();
            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            ErrorMessage = $"Не удалось сохранить изменения: {exception.Message}";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
