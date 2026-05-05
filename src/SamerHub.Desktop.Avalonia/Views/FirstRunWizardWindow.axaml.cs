using Avalonia.Controls;
using Avalonia.Interactivity;
using SamerHub.Core.Contracts;
using SamerHub.Desktop.Avalonia.Services;

namespace SamerHub.Desktop.Avalonia.Views;

public partial class FirstRunWizardWindow : Window
{
    private readonly AppSettingsService _settingsService;

    public FirstRunWizardWindow() : this(new AppSettingsService())
    {
    }

    public FirstRunWizardWindow(AppSettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        settings.SkipWizardOnStartup = SkipWizardCheckBox.IsChecked ?? false;
        settings.Database = new DatabaseConnectionSettings
        {
            Provider = MySqlRadio.IsChecked == true ? "MySql" : "Sqlite",
            Host = HostTextBox.Text ?? "localhost",
            Port = int.TryParse(PortTextBox.Text, out var port) ? port : 3306,
            Database = DatabaseTextBox.Text ?? "samerhub",
            Username = UsernameTextBox.Text ?? string.Empty,
            Password = PasswordTextBox.Text ?? string.Empty
        };
        _settingsService.Save(settings);
        Close();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
