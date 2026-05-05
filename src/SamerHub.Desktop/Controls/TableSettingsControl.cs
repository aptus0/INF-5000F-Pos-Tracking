namespace SamerHub.Desktop.Controls;

public sealed class TableSettingsControl : UserControl
{
    public TableSettingsControl()
    {
        BackColor = Color.White;
        Controls.Add(UiFactory.CreateEmptyState(
            "Salon, kat, masa kapasitesi ve masa sira tanimlari bu sekmede yonetilecek."));
    }
}
