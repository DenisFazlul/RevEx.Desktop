using RevEx.Domain.Contracts.ContentFile;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Модель строки файла версии. Подготавливает имя, назначение, размер и доступность редактирования.</summary>
public sealed class ContentVersionFileItemViewModel
{
    public int Id
    {
        get;
    }
    public string Name
    {
        get;
    }
    public string RoleName
    {
        get;
    }
    public string SizeText
    {
        get;
    }
    public bool CanEdit
    {
        get;
    }

    public ContentVersionFileItemViewModel(ContentFileDto file, bool canEdit = true)
    {
        CanEdit = canEdit;
        Id = file.Id;
        Name = file.OriginalFileName;
        RoleName = file.Role switch
        {
            "primary" => "Основной файл",
            "type-catalog" => "Каталог типов",
            "lookup-table" => "Таблица поиска",
            _ => "Вложение"
        };
        SizeText = file.SizeBytes switch
        {
            < 1024 => $"{file.SizeBytes} Б",
            < 1024 * 1024 => $"{file.SizeBytes / 1024d:0.#} КБ",
            _ => $"{file.SizeBytes / (1024d * 1024d):0.#} МБ"
        };
    }
}
