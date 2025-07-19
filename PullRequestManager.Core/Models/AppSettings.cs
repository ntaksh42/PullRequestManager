namespace AzureDevopsTool.Core.Models;

public class AppSettings
{
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string AuthorFilter { get; set; } = string.Empty;
    public string TargetBranchFilter { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 600;
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public bool[] ColumnVisibility { get; set; } = new bool[9] { true, true, true, true, true, true, true, true, true };
    public List<SavedSearch> SavedSearches { get; set; } = new List<SavedSearch>();
}