using System.Text.Json;
using SamerHub.Core.Contracts;

namespace SamerHub.Desktop.Avalonia.Services;

public sealed class AppSettingsService
{
    private readonly string _settingsFile;

    public AppSettingsService()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SAMER Hub");
        Directory.CreateDirectory(directory);
        _settingsFile = Path.Combine(directory, "desktop-settings.json");
    }

    public DesktopAppSettings Load()
    {
        if (!File.Exists(_settingsFile))
        {
            return new DesktopAppSettings();
        }

        var json = File.ReadAllText(_settingsFile);
        return JsonSerializer.Deserialize<DesktopAppSettings>(json) ?? new DesktopAppSettings();
    }

    public void Save(DesktopAppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFile, json);
    }

    public bool ShouldShowWizardOnStartup() => !Load().SkipWizardOnStartup;

    public void SetSkipWizardOnStartup(bool skip)
    {
        var settings = Load();
        settings.SkipWizardOnStartup = skip;
        Save(settings);
    }
}

public sealed class DesktopAppSettings
{
    public bool SkipWizardOnStartup { get; set; }
    public DatabaseConnectionSettings Database { get; set; } = new();
}
