using System.Text.Json;
using System.Text.Json.Nodes;

namespace RevEx.Configuration;

public sealed class RevExUserSettingsStore : IRevExUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string FilePath => RevExSettingsPaths.UserSettingsFile;

    public RevExUserSettings Load()
    {
        if (!File.Exists(FilePath))
            return new RevExUserSettings();

        var root = ReadRoot(FilePath);
        var appSettings = root["AppSettings"] as JsonObject;
        return new RevExUserSettings
        {
            ApiPath = appSettings?["ApiPath"]?.GetValue<string>(),
            ConnectorRegistrationAddress =
                appSettings?["ConnectorRegistrationAddress"]?.GetValue<string>()
        };
    }

    public void Save(RevExUserSettings settings)
    {
        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("Не удалось определить каталог настроек RevEx.");
        Directory.CreateDirectory(directory);

        var root = File.Exists(FilePath) ? ReadRoot(FilePath) : new JsonObject();
        var appSettings = root["AppSettings"] as JsonObject ?? new JsonObject();
        root["AppSettings"] = appSettings;
        appSettings["ApiPath"] = settings.ApiPath;
        appSettings["ConnectorRegistrationAddress"] = settings.ConnectorRegistrationAddress;

        var temporaryFile = $"{FilePath}.tmp";
        try
        {
            File.WriteAllText(temporaryFile, root.ToJsonString(JsonOptions));
            File.Move(temporaryFile, FilePath, true);
        }
        finally
        {
            if (File.Exists(temporaryFile))
                File.Delete(temporaryFile);
        }
    }

    private static JsonObject ReadRoot(string path) =>
        JsonNode.Parse(File.ReadAllText(path)) as JsonObject
        ?? throw new JsonException($"Корень файла '{path}' должен быть JSON-объектом.");
}
