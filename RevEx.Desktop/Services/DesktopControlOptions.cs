using System;
using System.IO;

namespace RevEx.Desktop.Services;

public sealed class DesktopControlOptions
{
    public DesktopControlOptions()
        : this(GetRegistrationFile())
    {
    }

    public DesktopControlOptions(string registrationFile)
    {
        if (string.IsNullOrWhiteSpace(registrationFile))
        {
            throw new ArgumentException("Registration file path is required.", nameof(registrationFile));
        }

        RegistrationFile = Path.GetFullPath(registrationFile);
    }

    public string RegistrationFile { get; }

    private static string GetRegistrationFile()
    {
        var configuredPath = Environment.GetEnvironmentVariable("REVEX_DESKTOP_REGISTRATION_FILE");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RevEx");

        return Path.Combine(settingsDirectory, "desktop-control.json");
    }
}
