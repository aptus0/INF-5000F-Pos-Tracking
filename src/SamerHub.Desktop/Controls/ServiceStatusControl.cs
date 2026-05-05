namespace SamerHub.Desktop.Controls;

public sealed class ServiceStatusControl : UserControl
{
    public ServiceStatusControl()
    {
        BackColor = Color.White;
        Controls.Add(UiFactory.CreateEmptyState(
            "Windows Service, local API, internet, POS ve yazici saglik durumu bu sekmede izlenecek."));
    }
}
