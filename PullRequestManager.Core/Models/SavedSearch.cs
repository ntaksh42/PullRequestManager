namespace AzureDevopsTool.Core.Models;

public class SavedSearch
{
    public string Name { get; set; } = string.Empty;
    public string AuthorFilter { get; set; } = string.Empty;
    public string TargetBranchFilter { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public int MinChanges { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return Name;
    }
}