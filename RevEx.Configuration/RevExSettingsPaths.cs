namespace RevEx.Configuration;

public static class RevExSettingsPaths
{
    private static readonly string SettingsDirectory = GetSettingsDirectory();

    public static string UserSettingsFile { get; } = Path.Combine(
        SettingsDirectory,
        "user-settings.json");

    public static string DownloadsDirectory { get; } = Path.Combine(
        SettingsDirectory,
        "Downloads");

    private static string GetSettingsDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "RevEx");
        }

        if (OperatingSystem.IsLinux())
        {
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            return Path.Combine(
                string.IsNullOrWhiteSpace(configHome)
                    ? Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".config")
                    : configHome,
                "RevEx");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RevEx");
    }
}
