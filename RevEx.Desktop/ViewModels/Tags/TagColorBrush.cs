using Avalonia.Media;

namespace RevEx.Desktop.ViewModels.Tags;

internal static class TagColorBrush
{
    public static IBrush? FromHex(string? value)
    {
        if (value is { Length: 7 } && value[0] == '#' && Color.TryParse(value, out var color))
        {
            return new SolidColorBrush(color);
        }

        return null;
    }
}
