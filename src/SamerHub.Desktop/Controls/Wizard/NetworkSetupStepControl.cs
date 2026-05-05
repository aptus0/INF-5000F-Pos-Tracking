namespace SamerHub.Desktop.Controls.Wizard;

public sealed class NetworkSetupStepControl : UserControl
{
    public NetworkSetupStepControl()
    {
        BackColor = Color.White;

        Controls.Add(UiFactory.CreateSubtitle(
            "PC IP, yerel ag yapisi ve POS ile ayni subnet icinde calisma kurallari bu adimda belirlenir.",
            new Point(0, 34)));
        Controls.Add(UiFactory.CreateTitle("2. Ag ve PC IP", new Point(0, 0)));

        var section = UiFactory.CreateSection("Ag Yapilandirmasi", new Point(0, 72), new Size(760, 260));
        section.Controls.Add(UiFactory.CreateEmptyState(
            "Bu adimda otomatik IP algilama, secili ag karti ve yerel subnet dogrulamasi uygulanacak.\r\n\r\nBir sonraki asamada POS IP ve port bilgileri girilecek."));
        Controls.Add(section);
    }
}
