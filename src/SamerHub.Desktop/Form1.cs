using SamerHub.Desktop.Controls;
using System.Reflection;

namespace SamerHub.Desktop;

public partial class Form1 : Form
{
    private readonly DashboardControl _dashboardControl = new();
    private readonly TablesControl _tablesControl = new();
    private readonly SettingsControl _settingsControl = new();
    private readonly ProductsControl _productsControl = new();

    public Form1()
    {
        InitializeComponent();
        Text = "SAMER Hub Desktop";
        LoadHeaderLogo();
        WireNavigation();
        NavigateTo(_dashboardControl, dashboardButton);
        UpdateClock();
        footerTimer.Start();
    }

    private void WireNavigation()
    {
        dashboardButton.Click += (_, _) => NavigateTo(_dashboardControl, dashboardButton);
        tablesButton.Click += (_, _) => NavigateTo(_tablesControl, tablesButton);
        productsButton.Click += (_, _) => NavigateTo(_productsControl, productsButton);
        settingsButton.Click += (_, _) => NavigateTo(_settingsControl, settingsButton);
    }

    private void NavigateTo(UserControl control, Button activeButton)
    {
        contentHostPanel.SuspendLayout();
        contentHostPanel.Controls.Clear();
        control.Dock = DockStyle.Fill;
        contentHostPanel.Controls.Add(control);
        contentHostPanel.ResumeLayout();
        SetActiveButton(activeButton);
    }

    private void SetActiveButton(Button activeButton)
    {
        foreach (var button in menuFlowPanel.Controls.OfType<Button>())
        {
            var isActive = button == activeButton;
            button.BackColor = isActive ? Color.FromArgb(33, 37, 41) : Color.White;
            button.ForeColor = isActive ? Color.White : Color.Black;
            button.FlatAppearance.BorderSize = isActive ? 0 : 1;
            button.Font = new Font("Segoe UI", 9F, isActive ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point, 162);
        }
    }

    private void footerTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
    }

    private void UpdateClock()
    {
        clockStatusLabel.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
    }

    private void LoadHeaderLogo()
    {
        try
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(assemblyDirectory, "logo.png"),
                Path.Combine(assemblyDirectory, "..", "..", "..", "Resources", "logo.png")
            };

            var logoPath = candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
            if (logoPath is not null)
            {
                logoPictureBox.Image = Image.FromFile(logoPath);
            }
        }
        catch
        {
        }
    }
}
