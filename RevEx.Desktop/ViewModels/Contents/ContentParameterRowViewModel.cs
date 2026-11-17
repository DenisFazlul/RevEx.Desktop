using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Domain.Contracts.Content;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentParameterRowViewModel : ObservableObject
{
    private readonly Func<int, ContentParameterInput, Task<int>> _save;
    private readonly Func<ContentParameterRowViewModel, Task> _delete;

    private static readonly IReadOnlyList<string> TypeLabels = ["Строка", "Число", "Ссылка"];

    public static FuncValueConverter<string, string> KindLabelConverter { get; } =
        new(kind => TypeLabels[GetTypeIndex(kind)]);

    public IReadOnlyList<string> Types => TypeLabels;
    public int Id { get; private set; }
    public bool CanEdit { get; }
    public bool IsExisting => Id != 0;
    public string ValueHint => string.IsNullOrWhiteSpace(Help) ? TypeHint : Help;
    public string TypeHint => TypeIndex switch
    {
        1 => "Введите число, например 12,5.",
        2 => "Введите полный адрес, начиная с https:// или http://.",
        _ => "Введите произвольный текст."
    };

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeHint), nameof(ValueHint))]
    private int _typeIndex;

    [ObservableProperty]
    private string _value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValueHint))]
    private string? _help;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isBusy;

    public bool IsIdle => !IsBusy;

    public ContentParameterRowViewModel(
        ContentParameterDto item,
        bool canEdit,
        Func<int, ContentParameterInput, Task<int>> save,
        Func<ContentParameterRowViewModel, Task> delete)
    {
        Id = item.Id;
        CanEdit = canEdit;
        _name = item.Name;
        _value = item.Value;
        _help = item.Help;
        _typeIndex = GetTypeIndex(item.Kind);
        _save = save;
        _delete = delete;
    }

    private static int GetTypeIndex(string? kind)
    {
        return kind switch
        {
            "number" => 1,
            "link" => 2,
            _ => 0
        };
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanEdit || IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            Message = "Укажите название.";
            return;
        }

        var kind = TypeIndex switch
        {
            1 => "number",
            2 => "link",
            _ => "string"
        };
        var value = Value ?? string.Empty;
        if (TypeIndex == 1)
        {
            if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var number)
                && !decimal.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                Message = TypeHint;
                return;
            }

            value = number.ToString(CultureInfo.InvariantCulture);
        }

        if (TypeIndex == 2)
        {
            value = value.Trim();
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https") || uri.Host.Length == 0)
            {
                Message = TypeHint;
                return;
            }
        }

        try
        {
            IsBusy = true;
            var input = new ContentParameterInput(Name.Trim(), kind, value, Help?.Trim());
            Id = await _save(Id, input);
            Name = input.Name;
            Value = input.Value;
            Help = input.Help;
            Message = "Сохранено.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            Message = $"Не удалось сохранить: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!CanEdit || IsBusy || !IsExisting)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _delete(this);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            Message = $"Не удалось удалить: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
