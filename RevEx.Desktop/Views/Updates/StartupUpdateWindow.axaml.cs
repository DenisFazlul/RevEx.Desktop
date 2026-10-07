using Avalonia.Controls;
using RevEx.Desktop.ViewModels.Updates;

namespace RevEx.Desktop.Views.Updates;

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
