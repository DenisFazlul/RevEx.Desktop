using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Domain.Contracts.Content;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentDetailsViewModel
{
    public ObservableCollection<ContentParameterDto> Parameters { get; } = [];

    [ObservableProperty]
    private ContentParameterRowViewModel? _parameterEditor;

    [ObservableProperty]
    private string? _parameterMessage;

    private async Task LoadParametersAsync()
    {
        try
        {
            var items = await _apiService.Contents.GetParametersAsync(Id);
            Parameters.Clear();
            foreach (var item in items)
            {
                Parameters.Add(item);
            }

            ParameterMessage = null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ParameterMessage = $"Не удалось загрузить параметры: {exception.Message}";
        }
    }

    [RelayCommand]
    private void AddParameter()
    {
        EditParameter(new ContentParameterDto(0, "", "string", "", null));
    }

    [RelayCommand]
    private void CancelParameter()
    {
        if (ParameterEditor?.IsBusy != true)
        {
            ParameterEditor = null;
        }
    }

    [RelayCommand]
    private void EditParameter(ContentParameterDto item)
    {
        if (!CanEdit || IsLoading || _content is null || ParameterEditor?.IsBusy == true)
        {
            return;
        }

        var contentId = Id;
        ParameterEditor = new ContentParameterRowViewModel(
            item,
            CanEdit,
            async (id, input) =>
            {
                if (id == 0)
                {
                    var created = await _apiService.Contents.CreateParameterAsync(contentId, input);
                    Parameters.Add(created);
                    ParameterEditor = null;
                    return created.Id;
                }

                await _apiService.Contents.UpdateParameterAsync(contentId, id, input);
                var index = Parameters.IndexOf(item);
                if (index >= 0)
                {
                    Parameters[index] = new ContentParameterDto(id, input.Name, input.Kind, input.Value, input.Help);
                }

                ParameterEditor = null;
                return id;
            },
            async row =>
            {
                await _apiService.Contents.DeleteParameterAsync(contentId, row.Id);
                Parameters.Remove(item);
                ParameterEditor = null;
            });
    }
}
