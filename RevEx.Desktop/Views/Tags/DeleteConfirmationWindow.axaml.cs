using Avalonia.Controls;

namespace RevEx.Desktop.Views.Tags;

public partial class DeleteConfirmationWindow : Window
{
    public string Message { get; }
    public bool CanDelete { get; }
    public string CancelButtonText => CanDelete ? "Отмена" : "Закрыть";

    public DeleteConfirmationWindow() : this(string.Empty, true) { }

    public DeleteConfirmationWindow(string message, bool canDelete = true)
    {
        Message = message;
        CanDelete = canDelete;
        InitializeComponent();
        DataContext = this;
    }

    private void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close(true);
    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close(false);
}
