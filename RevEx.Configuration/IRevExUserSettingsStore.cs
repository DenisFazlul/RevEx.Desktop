namespace RevEx.Configuration;

public interface IRevExUserSettingsStore
{
    string FilePath { get; }
    RevExUserSettings Load();
    void Save(RevExUserSettings settings);
}
