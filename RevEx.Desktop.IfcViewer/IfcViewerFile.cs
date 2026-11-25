namespace RevEx.Desktop.IfcViewer;

/// <summary>Определяет форматы, которые может открыть IFC-просмотрщик.</summary>
public static class IfcViewerFile
{
    public static bool IsSupported(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        return string.Equals(extension, ".ifc", StringComparison.OrdinalIgnoreCase);
    }
}
