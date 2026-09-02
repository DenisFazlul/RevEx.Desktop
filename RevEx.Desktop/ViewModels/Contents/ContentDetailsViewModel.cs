namespace RevEx.Desktop.ViewModels.Contents;

public sealed class ContentDetailsViewModel : ViewModelBase
{
    public int Id { get; }
    public string Title { get; }
    public string Description { get; }

    public ContentDetailsViewModel(ContentItemViewModel content)
    {
        Id = content.Id;
        Title = content.Name;
        Description = content.Description;
    }
}
