using CommunityToolkit.Mvvm.Input;

namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Модель ввода названия или описания. Проверяет текст и возвращает сохранённое значение.</summary>
public partial class TextInputViewModel : TextEditorViewModel<string>
{
    public TextInputViewModel(TextInputOptions options)
    {
        Options = options;
        Text = options.InitialValue;
    }

    public TextInputOptions Options
    {
        get;
    }

    [RelayCommand]
    private void Save()
    {
        if (TryReadText("Значение не должно быть пустым.", Options.MaximumLength, out var value))
        {
            Complete(value);
        }
    }
}
