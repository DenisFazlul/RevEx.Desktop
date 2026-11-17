using System;
using System.IO;
using Avalonia;
using Avalonia.Styling;
using RevEx.Configuration;

namespace RevEx.Desktop.Services;

internal static class DesktopThemePreference
{
    public const string Light = "Light";
    public const string Dark = "Dark";
    public const string LightTube = "Light Tube";
    public const string DarkTube = "Dark Tube";

    public static ThemeVariant LightTubeVariant { get; } = new(LightTube, ThemeVariant.Light);
    public static ThemeVariant DarkTubeVariant { get; } = new(DarkTube, ThemeVariant.Dark);

    private static readonly string SettingsDirectory = Path.GetDirectoryName(RevExSettingsPaths.UserSettingsFile)!;
    private static readonly string ThemeFile = Path.Combine(SettingsDirectory, "desktop-theme.txt");

    public static string Load()
    {
        try
        {
            return File.ReadAllText(ThemeFile).Trim();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return LightTube;
        }
    }

    public static void Apply(Application application, string theme)
    {
        application.RequestedThemeVariant = theme switch
        {
            Light => ThemeVariant.Light,
            Dark => ThemeVariant.Dark,
            DarkTube => DarkTubeVariant,
            _ => LightTubeVariant
        };
    }

    public static string GetName(ThemeVariant? variant)
    {
        return (variant?.Key as string) switch
        {
            Light => Light,
            Dark => Dark,
            DarkTube => DarkTube,
            _ => LightTube
        };
    }

    public static void Save(string theme)
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(ThemeFile, theme);
    }
}
