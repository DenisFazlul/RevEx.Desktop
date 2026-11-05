using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using RevEx.Desktop.Services;

namespace RevEx.Desktop.Views.Common;

/// <summary>Единый выбор локального файла для форм. Отменённый выбор возвращает null.</summary>
public static class LocalFilePicker
{
    public static FilePickerFileType RevitFamily
    {
        get;
    } = new("Семейство Revit (*.rfa)")
    {
        Patterns = ["*.rfa"]
    };

    public static async Task<SelectedLocalFile?> OpenAsync(Control view, string title, params FilePickerFileType[] types)
    {
        var topLevel = TopLevel.GetTopLevel(view);
        if (topLevel?.StorageProvider is null)
        {
            return null;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = types
        });
        var file = files.FirstOrDefault();
        var path = file?.TryGetLocalPath();
        return path is null || file is null ? null : new SelectedLocalFile(path, file.Name);
    }
}
