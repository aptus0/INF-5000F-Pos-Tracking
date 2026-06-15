namespace SamerHub.Desktop.Controls;

public sealed class BackupSettingsControl : UserControl
{
    public BackupSettingsControl()
    {
        BackColor = Color.White;
        Controls.Add(UiFactory.CreateEmptyState(
            "Veritabani yedekleme, geri yukleme ve otomatik yedek planlari bu sekmede yer alacak."));
    }
}
