using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentTagItemViewModel : ViewModelBase
{
    private readonly Func<ContentTagItemViewModel, Task> _remove;

    public int Id { get; }

    public string Name { get; }

    public ContentTagItemViewModel(TagDto dto, Func<ContentTagItemViewModel, Task> remove)
    {
        Id = dto.Id;
        Name = dto.Name;
        _remove = remove;
    }

    [RelayCommand]
    private Task RemoveAsync() => _remove(this);
}
