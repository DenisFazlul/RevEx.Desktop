namespace RevEx.Configuration;

public static class RevExSettingsPaths
{
    public static string UserSettingsFile { get; } = Path.Combine(
        GetSettingsDirectory(),
        "user-settings.json");

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
