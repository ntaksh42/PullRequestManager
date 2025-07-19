namespace AzureDevopsTool.Core.Models;

public class AzureDevOpsConfig
{
    public string Organization { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
}