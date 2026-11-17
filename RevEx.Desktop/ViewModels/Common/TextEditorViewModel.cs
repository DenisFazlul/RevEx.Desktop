using CommunityToolkit.Mvvm.ComponentModel;

namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Общая модель текстового поля: хранит ввод и проверяет обязательность и длину значения.</summary>
public abstract partial class TextEditorViewModel<TResult> : DialogViewModel<TResult>
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string? _validationMessage;

    protected bool TryReadText(string emptyMessage, int maximumLength, out string value)
    {
        value = Text.Trim();
        ValidationMessage = null;
        if (value.Length == 0)
        {
            ValidationMessage = emptyMessage;
            return false;
        }

        if (value.Length > maximumLength)
        {
            ValidationMessage = $"Допускается не больше {maximumLength} символов.";
            return false;
        }

        return true;
    }
}
