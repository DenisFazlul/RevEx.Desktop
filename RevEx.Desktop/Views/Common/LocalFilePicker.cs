using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using RevEx.Desktop.Services;

namespace RevEx.Desktop.Views.Common;

/// <summary>Выбор одного или нескольких локальных файлов. Отмена возвращает null или пустой список.</summary>
public static class LocalFilePicker
{
    public static FilePickerFileType RevitFamily
    {
        get;
    } = new("Семейство Revit (*.rfa)")
    {
        Patterns = ["*.rfa"]
    };

    public static FilePickerFileType RevitFamilyWithAuxiliaryFiles
    {
        get;
    } = new("Семейство Revit и вспомогательные файлы (*.rfa, *.csv, *.ifc)")
    {
        Patterns = ["*.rfa", "*.csv", "*.ifc"]
    };

    public static async Task<IReadOnlyList<SelectedLocalFile>> OpenMultipleAsync(
        Control view, string title, params FilePickerFileType[] types)
    {
        var topLevel = TopLevel.GetTopLevel(view);
        if (topLevel?.StorageProvider is null)
        {
            return [];
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = types
        });
        var selectedFiles = new List<SelectedLocalFile>();
        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path is not null)
            {
                selectedFiles.Add(new SelectedLocalFile(path, file.Name));
            }
        }

        return selectedFiles;
    }

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
