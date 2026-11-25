using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentFile;
using RevEx.Domain.Contracts.ContentHistory;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Contracts.Tag;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Connector.Contracts;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Configuration;
using RevEx.Connector.Client;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Назначение и снятие тегов карточки, обновление связанной истории.</summary>
public partial class ContentDetailsViewModel
{
    public async Task AssignTagsAsync(IReadOnlyList<TagDto> tags)
    {
        if (!CanEdit || _content is null || IsSavingTags)
        {
            return;
        }

        var tagIds = Tags.Select(item => item.Id).ToHashSet();
        var newTags = tags.Where(tag => tagIds.Add(tag.Id)).ToArray();
        if (newTags.Length == 0)
        {
            return;
        }

        await RunTagActionAsync(async () =>
        {
            await UpdateTagAssignmentsAsync(tagIds.ToArray());
            foreach (var tag in newTags)
            {
                Tags.Add(new ContentTagItemViewModel(tag, RemoveTagAsync));
            }

            TagMessage = newTags.Length == 1
                ? $"Тег «{newTags[0].Name}» добавлен."
                : $"Добавлено тегов: {newTags.Length}.";
            NotifyCollectionStateChanged();
        });
    }

    public async Task<IReadOnlyCollection<TagDto>> GetAvailableTagsAsync()
    {
        try
        {
            ErrorMessage = null;
            _allTags = await _apiService.Tags.GetAllAsync();
            NotifyCollectionStateChanged();
            return AvailableTags;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось загрузить доступные теги: {exception.Message}";
            return [];
        }
    }

    private async Task RemoveTagAsync(ContentTagItemViewModel tag)
    {
        if (!CanEdit || _content is null)
        {
            return;
        }

        await RunTagActionAsync(async () =>
        {
            var tagIds = Tags.Where(item => item.Id != tag.Id).Select(item => item.Id).ToArray();
            await UpdateTagAssignmentsAsync(tagIds);
            Tags.Remove(tag);
            TagMessage = $"Тег «{tag.Name}» снят с контента.";
            NotifyCollectionStateChanged();
        });
    }

    private async Task UpdateTagAssignmentsAsync(IReadOnlyCollection<int> tagIds)
    {
        if (!CanEdit || _content is null)
        {
            return;
        }

        await _apiService.Contents.UpdateAsync(
            Id,
            CreateUpdateRequest(_content.Name, _content.Description, tagIds));
        _content = _content with
        {
            TagIds = tagIds,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await RefreshHistoryAsync();
    }

    private async Task RunTagActionAsync(Func<Task> action)
    {
        try
        {
            IsSavingTags = true;
            ErrorMessage = null;
            TagMessage = null;
            await action();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось изменить теги: {exception.Message}";
        }
        finally
        {
            IsSavingTags = false;
        }
    }

}
