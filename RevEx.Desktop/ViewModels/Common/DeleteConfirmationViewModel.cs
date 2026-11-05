using CommunityToolkit.Mvvm.Input;

namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Модель подтверждения удаления. Показывает последствия и возвращает решение пользователя.</summary>
public partial class DeleteConfirmationViewModel : DialogViewModel<bool>
{
    public DeleteConfirmationViewModel(string message, bool canDelete = true) : base(false)
    {
        Description = message;
        CanDelete = canDelete;
    }

    public string Description
    {
        get;
    }
    public bool CanDelete
    {
        get;
    }
    public string CancelButtonText => CanDelete ? "Отмена" : "Закрыть";

    [RelayCommand]
    private void Delete()
    {
        if (CanDelete)
        {
            Complete(true);
        }
    }
}
