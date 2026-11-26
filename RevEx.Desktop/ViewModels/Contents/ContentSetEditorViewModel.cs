using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.ContentSet;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentSetEditorViewModel : DialogViewModelBase
{
    private readonly IContentSetRepository _sets;
    private readonly int? _id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    public string Title => _id.HasValue ? "Изменить набор" : "Создать набор";
    public string? Author { get; }

    public ContentSetEditorViewModel(IContentSetRepository sets, ContentSetDto? set = null)
    {
        _sets = sets;
        _id = set?.Id;
        _name = set?.Name ?? string.Empty;
        _description = set?.Description ?? string.Empty;
        Author = set?.Author;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 200 ||
            string.IsNullOrWhiteSpace(Description) || Description.Length > 10000)
        {
            SetError("Укажите название (до 200 символов) и описание (до 10000 символов).");
            return;
        }

        var id = _id;
        var saved = await RunSaveAsync(async () =>
        {
            var request = new SaveContentSetDto(Name.Trim(), Description.Trim());
            if (id.HasValue)
            {
                await _sets.UpdateAsync(id.Value, request);
            }
            else
            {
                var created = await _sets.CreateAsync(request);
                id = created.Id;
            }
        }, "Не удалось сохранить набор");

        if (saved)
        {
            RequestClose(id);
        }
    }
}
