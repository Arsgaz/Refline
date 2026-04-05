namespace Refline.Admin.ViewModels;

public sealed class PlaceholderViewModel : ViewModelBase
{
    public PlaceholderViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }
}
