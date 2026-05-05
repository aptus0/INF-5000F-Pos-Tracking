using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SamerHub.Desktop.Avalonia.Services;
using SamerHub.Desktop.Avalonia.ViewModels;
using SamerHub.Desktop.Avalonia.Views;
using SamerHub.Core.Contracts;

namespace SamerHub.Desktop.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var appSettings = new AppSettingsService();
                var startup = ResolveStartup(appSettings);
                var viewModel = new MainWindowViewModel(new SamerHubApiClient(), appSettings, startup);
                var window = new MainWindow { DataContext = viewModel };
                desktop.MainWindow = window;

                if (appSettings.ShouldShowWizardOnStartup())
                {
                    var wizard = new FirstRunWizardWindow(appSettings);
                    wizard.Show(window);
                }
            }
            catch (Exception ex)
            {
                DesktopStartupDiagnostics.LogError("Unhandled exception in App.OnFrameworkInitializationCompleted", ex);
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(
                        new SamerHubApiClient(),
                        new AppSettingsService(),
                        new ServiceStartupResult(
                            false,
                            $"Desktop startup failed: {ex.Message}",
                            null))
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceStartupResult ResolveStartup(AppSettingsService appSettings)
    {
        try
        {
            var bootstrapper = new ServiceBootstrapper(appSettings);
            var startupTask = bootstrapper.EnsureServiceAvailableAsync();
            var completed = Task.WhenAny(startupTask, Task.Delay(TimeSpan.FromSeconds(5))).GetAwaiter().GetResult();
            if (completed == startupTask)
            {
                return startupTask.GetAwaiter().GetResult();
            }

            DesktopStartupDiagnostics.LogInfo("Service bootstrap timed out. Main window will open in offline mode.");
            return new ServiceStartupResult(false, "Servis zaman asimina ugradi. Pencere offline modda acildi.", null);
        }
        catch (Exception ex)
        {
            DesktopStartupDiagnostics.LogError("Service bootstrap failed", ex);
            return new ServiceStartupResult(false, $"Servis baslatma hatasi: {ex.Message}", null);
        }
    }
}
