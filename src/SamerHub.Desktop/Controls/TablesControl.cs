namespace SamerHub.Desktop.Controls;

public sealed class TablesControl : UserControl
{
    public TablesControl()
    {
        BackColor = Color.WhiteSmoke;

        Controls.Add(UiFactory.CreateSubtitle(
            "Salon ve bolum bazli masa yonetimi burada olacak. Gercek masa verileri ayarlar ve veritabani tarafindan beslenecek.",
            new Point(3, 34)));
        Controls.Add(UiFactory.CreateTitle("Masalar", new Point(0, 0)));

        var infoPanel = new Panel
        {
            Location = new Point(0, 72),
            Size = new Size(1180, 54),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        infoPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(16, 17),
            ForeColor = SystemColors.GrayText,
            Text = "Salon filtreleri ve masa ekleme akisi, gercek veri geldikce bu alanda sade bir toolbar olarak gosterilecek."
        });

        var section = UiFactory.CreateSection("Masa Alani", new Point(0, 142), new Size(1180, 353));
        section.Controls.Add(UiFactory.CreateEmptyState(
            "Henuz tanimli masa veya salon bulunmuyor.\r\n\r\nAyarlar > Masa / Salon ekranindan yeni salon ve masa tanimlari yapildiginda bu alan kart gorunumune donecek."));

        Controls.Add(section);
        Controls.Add(infoPanel);
    }
}
