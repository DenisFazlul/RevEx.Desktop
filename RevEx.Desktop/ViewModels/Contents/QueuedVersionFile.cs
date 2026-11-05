using System;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Данные файла для фоновой загрузки в приложение: идентификаторы, имя и локальный путь.</summary>
internal sealed record QueuedVersionFile(
    int ContentFileId,
    Guid LoadingId,
    string FileName,
    string Path);
