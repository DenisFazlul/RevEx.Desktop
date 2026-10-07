namespace RevEx.Desktop.Services.Updates;

public sealed class UpdateSettings
{
    public bool Enabled { get; set; } = true;
    public string RepositoryUrl { get; set; } = "https://github.com/DenisFazlul/RevEx.Desktop";
    public int CheckTimeoutSeconds { get; set; } = 5;
}
