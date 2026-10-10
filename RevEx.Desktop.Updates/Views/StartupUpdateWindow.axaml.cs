using Avalonia.Controls;
using RevEx.Desktop.Updates.ViewModels;

namespace RevEx.Desktop.Updates.Views;

public partial class StartupUpdateWindow : Window
{
    public StartupUpdateWindow()
    {
        InitializeComponent();
        Closing += (_, args) =>
        {
            if (DataContext is not StartupUpdateViewModel viewModel) return;
            args.Cancel = viewModel.IsBusy;
            viewModel.RequestClose();
        };
    }
}
