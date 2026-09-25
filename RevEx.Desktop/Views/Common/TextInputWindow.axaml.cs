using Avalonia.Controls;

namespace RevEx.Desktop.Views.Common;

public sealed record TextInputOptions(
    string WindowTitle,
    string FormTitle,
    string FieldLabel,
    string Hint,
    string InitialValue,
    bool IsMultiline = false,
    int MaximumLength = 5000);

public partial class TextInputWindow : Window
{
    public string WindowTitle { get; }
    public string FormTitle { get; }
    public string FieldLabel { get; }
    public string Hint { get; }
    public string InitialValue { get; }
    public bool IsMultiline { get; }
    public int MaximumLength { get; }
    public TextInputWindow() : this(new TextInputOptions("Ввод", "Введите значение", "Значение", string.Empty, string.Empty)) { }

    public TextInputWindow(TextInputOptions options)
    {
        WindowTitle = options.WindowTitle;
        FormTitle = options.FormTitle;
        FieldLabel = options.FieldLabel;
        Hint = options.Hint;
        InitialValue = options.InitialValue;
        IsMultiline = options.IsMultiline;
        MaximumLength = options.MaximumLength;
        InitializeComponent();
        DataContext = this;
        Opened += (_, _) => ValueBox.Focus();
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        var value = ValueBox.Text?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            ValidationMessage.Text = "Значение не должно быть пустым.";
            return;
        }

        Close(value);
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close(null);
}
