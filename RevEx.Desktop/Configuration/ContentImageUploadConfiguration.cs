using System;
using System.IO;
using Avalonia.Platform.Storage;

namespace RevEx.Desktop.Configuration;

public static class ContentImageUploadConfiguration
{
    public static FilePickerFileType FilePickerType { get; } = new("Изображения")
    {
        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp", "*.gif"],
        MimeTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"]
    };

    public static bool IsSupported(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
}
