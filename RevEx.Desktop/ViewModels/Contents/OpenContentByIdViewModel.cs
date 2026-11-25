using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.Content;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class OpenContentByIdViewModel : DialogViewModelBase
{
    private readonly IContentRepository _contents;
    private readonly Action<ContentDto> _openContent;

    [ObservableProperty]
    private string _identifiers = string.Empty;

    public OpenContentByIdViewModel(IContentRepository contents, Action<ContentDto> openContent)
    {
        _contents = contents;
        _openContent = openContent;
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        ClearMessage();
        var parts = Identifiers.Split(
            new[] { ' ', '\t', '\r', '\n', ',', ';' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            ErrorMessage = "Введите хотя бы один ID контента.";
            return;
        }

        var ids = new List<int>();
        var seen = new HashSet<int>();
        foreach (var part in parts)
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            {
                ErrorMessage = $"Некорректный ID: {part}. Используйте целые положительные числа.";
                return;
            }

            if (seen.Add(id))
            {
                ids.Add(id);
            }
        }

        IsLoading = true;
        try
        {
            var errors = new List<string>();
            var opened = 0;
            foreach (var id in ids)
            {
                try
                {
                    var content = await _contents.GetByIdAsync(id);
                    if (content is null)
                    {
                        errors.Add($"ID {id}: контент не найден или недоступен.");
                        continue;
                    }

                    _openContent(content);
                    opened++;
                }
                catch (Exception exception) when (IsExpectedError(exception))
                {
                    errors.Add($"ID {id}: не удалось открыть контент. {exception.Message}");
                }
            }

            if (errors.Count > 0)
            {
                SetError(string.Join(Environment.NewLine, errors));
                Message = $"Открыто контентов: {opened} из {ids.Count}.";
                return;
            }

            IsLoading = false;
            RequestClose(null);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
