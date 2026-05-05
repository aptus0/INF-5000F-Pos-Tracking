namespace SamerHub.Desktop.Controls;

public sealed class GeneralSettingsControl : UserControl
{
    public GeneralSettingsControl()
    {
        BackColor = Color.White;
        Controls.Add(UiFactory.CreateEmptyState(
            "Isletme adi, sube bilgileri, para birimi ve genel davranis ayarlari burada duzenlenecek."));
    }
}
