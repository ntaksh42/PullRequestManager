using System.Text.Json;
using AzureDevopsTool.Core.Models;

namespace AzureDevopsTool.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly IEncryptionService _encryptionService;

    public SettingsService() : this(new WindowsEncryptionService())
    {
    }

    public SettingsService(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "AzureDevopsTool");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            
            if (settings != null && !string.IsNullOrEmpty(settings.PersonalAccessToken))
            {
                // Decrypt the PAT
                settings.PersonalAccessToken = _encryptionService.Decrypt(settings.PersonalAccessToken);
            }
            
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            // Create a copy for encryption to avoid modifying the original
            var settingsToSave = new AppSettings
            {
                Organization = settings.Organization,
                Project = settings.Project,
                Repository = settings.Repository,
                PersonalAccessToken = string.IsNullOrEmpty(settings.PersonalAccessToken) 
                    ? string.Empty 
                    : _encryptionService.Encrypt(settings.PersonalAccessToken),
                AuthorFilter = settings.AuthorFilter,
                TargetBranchFilter = settings.TargetBranchFilter,
                FromDate = settings.FromDate,
                WindowWidth = settings.WindowWidth,
                WindowHeight = settings.WindowHeight,
                WindowLeft = settings.WindowLeft,
                WindowTop = settings.WindowTop,
                ColumnVisibility = settings.ColumnVisibility
            };

            var json = JsonSerializer.Serialize(settingsToSave, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch
        {
            // Silent fail - settings save is not critical
        }
    }
}