namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Модель диалога с заданным типом результата. Передаёт выбор пользователя вызывающему экрану.</summary>
public abstract class DialogViewModel<TResult> : DialogViewModelBase
{
    protected DialogViewModel(object? cancelResult = null) : base(cancelResult)
    {
    }

    protected void Complete(TResult result)
    {
        RequestClose(result);
    }
}
