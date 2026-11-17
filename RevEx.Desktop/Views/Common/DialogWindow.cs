using System;
using Avalonia.Controls;
using RevEx.Desktop.ViewModels.Common;

namespace RevEx.Desktop.Views.Common;

/// <summary>Связывает запрос закрытия модели с окном и освобождает ресурсы диалога.</summary>
public class DialogWindow : Window
{
    private DialogViewModelBase? _viewModel;

    public DialogWindow()
    {
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    protected override void OnClosing(WindowClosingEventArgs eventArgs)
    {
        if (_viewModel?.IsBusy == true)
        {
            eventArgs.Cancel = true;
        }

        base.OnClosing(eventArgs);
    }

    private void OnDataContextChanged(object? sender, EventArgs eventArgs)
    {
        DetachViewModel();
        _viewModel = DataContext as DialogViewModelBase;
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested += Close;
        }
    }

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        var disposable = _viewModel as IDisposable;
        DetachViewModel();
        disposable?.Dispose();
        DataContextChanged -= OnDataContextChanged;
        Closed -= OnClosed;
    }

    private void DetachViewModel()
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= Close;
            _viewModel = null;
        }
    }
}
