namespace RevEx.Desktop.ViewModels.Contents;

public sealed class ContentDetailsViewModel : Tabs.WorkspaceTabViewModel
{
    public int Id { get; }
    public string Description { get; }

    public ContentDetailsViewModel(ContentItemViewModel content) : base(content.Name, true, content.Id)
    {
        Id = content.Id;
        Description = content.Description;
    }
}
