namespace SamerHub.Desktop.Controls;

public sealed class PrinterSettingsControl : UserControl
{
    public PrinterSettingsControl()
    {
        BackColor = Color.White;
        Controls.Add(UiFactory.CreateEmptyState(
            "Mutfak, kasa ve paket yazicilari ile test fisi islemleri bu sekmede yer alacak."));
    }
}
