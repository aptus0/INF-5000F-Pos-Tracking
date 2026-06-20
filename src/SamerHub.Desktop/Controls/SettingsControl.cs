namespace SamerHub.Desktop.Controls;

public sealed class SettingsControl : UserControl
{
    public SettingsControl()
    {
        BackColor = Color.WhiteSmoke;

        var wizardButton = new Button
        {
            Location = new Point(1030, 10),
            Size = new Size(150, 38),
            Text = "Ilk Kurulum"
        };
        wizardButton.Click += (_, _) =>
        {
            using var wizard = new Forms.FirstRunWizardForm();
            wizard.ShowDialog(FindForm());
        };

        Controls.Add(UiFactory.CreateSubtitle(
            "Su an yalnizca gercekten kullanilabilen POS, platform ve yazici ayarlari gosteriliyor.",
            new Point(3, 34)));
        Controls.Add(UiFactory.CreateTitle("Ayarlar", new Point(0, 0)));
        Controls.Add(wizardButton);

        Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Location = new Point(0, 74),
            Text = "Not: Hazir olmayan ayar modulleri bu surumde gizlendi."
        });

        var tabControl = new TabControl
        {
            Location = new Point(0, 102),
            Size = new Size(1180, 404)
        };

        tabControl.TabPages.Add(CreateTab("POS Entegrasyonu", new PosIntegrationSettingsControl()));
        tabControl.TabPages.Add(CreateTab("Platform Baglantilari", new PlatformSettingsControl()));
        tabControl.TabPages.Add(CreateTab("Yazici", new PrinterSettingsControl()));

        Controls.Add(tabControl);
    }

    private static TabPage CreateTab(string title, UserControl control)
    {
        var tabPage = new TabPage(title);
        control.Dock = DockStyle.Fill;
        tabPage.Controls.Add(control);
        return tabPage;
    }
}
