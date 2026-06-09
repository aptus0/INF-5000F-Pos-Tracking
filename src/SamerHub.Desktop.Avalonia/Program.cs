using Avalonia;
using System.IO;

namespace SamerHub.Desktop.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            DesktopStartupDiagnostics.LogInfo("Desktop application bootstrapping.");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            DesktopStartupDiagnostics.LogError("Unhandled exception in Program.Main", ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                UseDBusMenu = true
            })
            .LogToTrace();
}

internal static class DesktopStartupDiagnostics
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SAMER Hub",
        "Logs");

    private static readonly string LogFilePath = Path.Combine(LogDirectory, "desktop-startup.log");

    public static void LogInfo(string message) => Write("INFO", message);

    public static void LogError(string message, Exception ex) =>
        Write("ERROR", $"{message}: {ex.Message}{Environment.NewLine}{ex}");

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(
                LogFilePath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}
