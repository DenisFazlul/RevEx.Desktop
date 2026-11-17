using Avalonia.Controls;

namespace RevEx.Desktop.Views.Common;

/// <summary>Находит модель представления и окно-владельца для открытия дочернего диалога.</summary>
public static class ViewContext
{
    public static bool TryGetContext<TViewModel>(this Control view, out Window owner, out TViewModel viewModel)
        where TViewModel : class
    {
        owner = null!;
        viewModel = null!;
        if (view.DataContext is not TViewModel currentViewModel ||
            TopLevel.GetTopLevel(view) is not Window currentOwner)
        {
            return false;
        }

        owner = currentOwner;
        viewModel = currentViewModel;
        return true;
    }
}
