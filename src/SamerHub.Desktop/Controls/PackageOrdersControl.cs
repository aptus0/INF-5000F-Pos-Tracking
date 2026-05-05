namespace SamerHub.Desktop.Controls;

public sealed class PackageOrdersControl : UserControl
{
    public PackageOrdersControl()
    {
        BackColor = Color.WhiteSmoke;

        Controls.Add(UiFactory.CreateSubtitle(
            "Trendyol Go, Getir, Yemeksepeti ve telefon siparisleri bu ekranda tek akista yonetilecek.",
            new Point(3, 34)));
        Controls.Add(UiFactory.CreateTitle("Paket Siparisler", new Point(0, 0)));

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
            Text = "Platform filtreleri, siparisler gercek entegrasyonla geldikten sonra tek satirlik sade filtre cubugu olarak gosterilecek."
        });

        var section = UiFactory.CreateSection("Siparis Listesi", new Point(0, 142), new Size(1180, 353));
        section.Controls.Add(UiFactory.CreateEmptyState("Aktif platform siparisi henuz yok. Platform baglantilari ayarlaninca siparisler burada listelenecek."));

        Controls.Add(section);
        Controls.Add(infoPanel);
    }
}
