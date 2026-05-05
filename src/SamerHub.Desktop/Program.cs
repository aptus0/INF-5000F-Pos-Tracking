using SamerHub.Desktop.Services;

namespace SamerHub.Desktop;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        bool showWizard = args.Any(x => x.Equals("--wizard", StringComparison.OrdinalIgnoreCase))
            || AppSettings.ShouldShowWizardOnStartup();

        if (showWizard)
        {
            var wizardResult = new Forms.FirstRunWizardForm().ShowDialog();
            if (wizardResult != DialogResult.OK)
            {
                return; // Closed or cancelled
            }
        }

        Application.Run(new Form1());
    }
}
