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

/// <summary>Изменение названия, описания, категории и превью карточки.</summary>
public partial class ContentDetailsViewModel
{
    public async Task UpdatePreviewAsync(string path, string fileName)
    {
        if (_content is null)
        {
            return;
        }

        try
        {
            IsUpdatingPreview = true;
            ErrorMessage = null;
            PreviewMessage = "Загружаем изображение…";
            await using var stream = File.OpenRead(path);
            _content = await _apiService.Contents.UploadPreviewAsync(Id, stream, fileName);
            await LoadPreviewAsync(_content.PreviewFileId);
            await RefreshHistoryAsync();
            PreviewMessage = "Изображение обновлено.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Не удалось обновить изображение: {exception.Message}";
            PreviewMessage = null;
        }
        finally
        {
            IsUpdatingPreview = false;
        }
    }

    public async Task UpdateContentInformationAsync(string name, string description)
    {
        if (_content is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        {
            ErrorMessage = "Название и описание не должны быть пустыми.";
            return;
        }

        try
        {
            IsSavingContent = true;
            ErrorMessage = null;
            ContentMessage = null;
            name = name.Trim();
            description = description.Trim();
            await _apiService.Contents.UpdateAsync(Id, CreateUpdateRequest(name, description, _content.TagIds));
            _content = _content with
            {
                Name = name,
                Description = description,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            Name = name;
            Description = description;
            Title = name;
            _contentUpdated(name, description, _content.CategoryId);
            await RefreshHistoryAsync();
            ContentMessage = "Изменения сохранены.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось сохранить контент: {exception.Message}";
        }
        finally
        {
            IsSavingContent = false;
        }
    }

    public Task UpdateContentNameAsync(string name) =>
        UpdateContentInformationAsync(name, Description);

    public Task UpdateContentDescriptionAsync(string description) =>
        UpdateContentInformationAsync(Name, description);

    public IReadOnlyCollection<CategoryDto> GetCategories() => _categories;
    public int? GetCurrentCategoryId() => _content?.CategoryId;

    public async Task UpdateContentCategoryAsync(CategoryDto category)
    {
        if (_content is null || _content.CategoryId == category.Id)
        {
            return;
        }

        try
        {
            IsSavingContent = true;
            ErrorMessage = null;
            ContentMessage = null;
            await _apiService.Contents.UpdateAsync(
                Id,
                CreateUpdateRequest(_content.Name, _content.Description, _content.TagIds, category.Id));
            _content = _content with
            {
                CategoryId = category.Id,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            CategoryName = category.Name;
            _contentUpdated(_content.Name, _content.Description, category.Id);
            await RefreshHistoryAsync();
            ContentMessage = "Категория изменена.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось изменить категорию: {exception.Message}";
        }
        finally
        {
            IsSavingContent = false;
        }
    }

    public async Task DeleteContentAsync()
    {
        try
        {
            IsDeleting = true;
            ErrorMessage = null;
            await _apiService.Contents.DeleteAsync(Id);
            _close();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось удалить контент: {exception.Message}";
        }
        finally
        {
            IsDeleting = false;
        }
    }

}
