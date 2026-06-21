namespace SamerHub.Desktop.Controls;

public sealed class UsersSettingsControl : UserControl
{
    public UsersSettingsControl()
    {
        BackColor = Color.White;
        Controls.Add(UiFactory.CreateEmptyState(
            "Kullanici, rol ve yetki tanimlari bu sekmede yonetilecek."));
    }
}
