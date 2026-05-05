using System.Text.Json;

namespace SamerHub.Desktop.Services;

public static class AppSettings
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SAMER Hub");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "app-settings.json");

    public static bool ShouldShowWizardOnStartup()
    {
        if (!File.Exists(SettingsFile))
            return true; // First launch - show wizard

        try
        {
            var json = File.ReadAllText(SettingsFile);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("SkipWizardOnStartup", out var skip))
            {
                return !skip.GetBoolean();
            }
        }
        catch { }

        return true; // Default to showing wizard if any error
    }

    public static void SetSkipWizardOnStartup(bool skip)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);

            var settings = new Dictionary<string, object> { { "SkipWizardOnStartup", skip } };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }
}
