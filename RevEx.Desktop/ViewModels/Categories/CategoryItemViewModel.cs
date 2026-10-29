using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Domain.Contracts.Category;

namespace RevEx.Desktop.ViewModels.Categories;

public partial class CategoryItemViewModel : ViewModelBase
{
    public int Id { get; }
    public bool CanModify => Id != 1;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isSelected;

    public CategoryItemViewModel(CategoryDto dto)
    {
        Id = dto.Id;
        _name = dto.Name;
    }
}
