using AzureDevopsTool.Core.Models;

namespace AzureDevopsTool.Core.Services;

public interface ISettingsService
{
    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}