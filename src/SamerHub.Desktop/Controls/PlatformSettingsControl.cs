namespace SamerHub.Desktop.Controls;

public sealed class PlatformSettingsControl : UserControl
{
    public PlatformSettingsControl()
    {
        BackColor = Color.White;
        Controls.Add(UiFactory.CreateEmptyState(
            "Trendyol Go, Getir ve Yemeksepeti API ayarlari, merchant kimlikleri ve baglanti testleri bu sekmede olacak."));
    }
}
