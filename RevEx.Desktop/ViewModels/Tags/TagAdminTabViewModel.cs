using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.ViewModels.Tabs;
using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Модель справочника групп и тегов. Загружает данные и сохраняет результаты редакторов.</summary>
public class TagAdminTabViewModel(IRevExApiService apiService)
    : WorkspaceTabViewModel("Группы тегов", true, "tags")
{
    public ObservableCollection<TagGroupItemViewModel> Groups { get; } = [];
    public bool HasGroups => Groups.Count > 0;
    public bool HasNoGroups => !HasGroups && !IsLoading;

    public override async Task ActivateAsync()
    {
        await RunLoadAsync(async () =>
        {
            var groupsTask = apiService.TagGroups.GetAllAsync();
            var tagsTask = apiService.Tags.GetAllAsync();
            await Task.WhenAll(groupsTask, tagsTask);
            var tagsByGroup = (await tagsTask).ToLookup(tag => tag.TagGroupId);
            Groups.ReplaceWith((await groupsTask).OrderBy(group => group.Name).Select(dto =>
            {
                var group = new TagGroupItemViewModel(dto);
                group.Tags.ReplaceWith(tagsByGroup[group.Id].OrderBy(tag => tag.Name).Select(tag => new TagItemViewModel(tag)));
                return group;
            }));
            NotifyStateChanged();
        }, "Не удалось загрузить группы тегов");
    }

    public Task<bool> AddGroupFromDialogAsync(string name)
    {
        return SaveChangesAsync(async () =>
        {
            var created = await apiService.TagGroups.CreateAsync(new CreateTagGroupDto { Name = name.Trim() });
            Groups.Add(new TagGroupItemViewModel(created));
            Message = "Группа создана.";
        });
    }

    public Task<bool> EditGroupFromDialogAsync(TagGroupItemViewModel group, string name)
    {
        return SaveChangesAsync(async () =>
        {
            await apiService.TagGroups.UpdateAsync(group.Id, new UpdateTagGroupDto { Name = name.Trim() });
            group.Name = name.Trim();
            Message = "Группа переименована.";
        });
    }

    public Task<bool> DeleteGroupFromDialogAsync(TagGroupItemViewModel group)
    {
        return SaveChangesAsync(async () =>
        {
            if (group.HasTags)
            {
                throw new InvalidOperationException("Сначала удалите или перенесите теги группы.");
            }

            await apiService.TagGroups.DeleteAsync(group.Id);
            Groups.Remove(group);
            Message = "Группа удалена.";
        });
    }

    public Task<bool> AddTagFromDialogAsync(string name, TagGroupItemViewModel group)
    {
        return SaveChangesAsync(async () =>
        {
            var created = await apiService.Tags.CreateAsync(new CreateTagDto { Name = name.Trim(), TagGroupId = group.Id });
            group.Tags.Add(new TagItemViewModel(created));
            Message = "Тег создан.";
        });
    }

    public Task<bool> EditTagFromDialogAsync(TagItemViewModel tag, string name, TagGroupItemViewModel targetGroup)
    {
        return SaveChangesAsync(async () =>
        {
            var oldGroup = Groups.First(group => group.Id == tag.TagGroupId);
            await apiService.Tags.UpdateAsync(tag.Id, new UpdateTagDto { Name = name.Trim(), TagGroupId = targetGroup.Id });
            tag.Name = name.Trim();
            if (oldGroup.Id != targetGroup.Id)
            {
                oldGroup.Tags.Remove(tag);
                tag.TagGroupId = targetGroup.Id;
                targetGroup.Tags.Add(tag);
            }

            Message = "Тег сохранён.";
        });
    }

    public Task<bool> DeleteTagFromDialogAsync(TagItemViewModel tag)
    {
        return SaveChangesAsync(async () =>
        {
            await apiService.Tags.DeleteAsync(tag.Id);
            var group = Groups.First(item => item.Id == tag.TagGroupId);
            group.Tags.Remove(tag);
            Message = "Тег удалён.";
        });
    }

    protected override void OnLoadingStateChanged()
    {
        NotifyStateChanged();
    }

    private Task<bool> SaveChangesAsync(Func<Task> action)
    {
        return RunSaveAsync(async () =>
        {
            await action();
            NotifyStateChanged();
        }, "Не удалось сохранить изменения");
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasGroups));
        OnPropertyChanged(nameof(HasNoGroups));
    }
}
