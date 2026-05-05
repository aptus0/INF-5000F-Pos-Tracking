namespace SamerHub.Desktop.Controls;

public sealed class DashboardControl : UserControl
{
    public DashboardControl()
    {
        BackColor = Color.WhiteSmoke;

        Controls.Add(UiFactory.CreateSubtitle(
            "Gercek siparis, masa, kasa ve POS verileri servis baglantisi tamamlandikca bu ekranda canli gosterilecek.",
            new Point(3, 34)));
        Controls.Add(UiFactory.CreateTitle("Dashboard", new Point(0, 0)));

        var statsPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 72),
            Size = new Size(1180, 94)
        };

        statsPanel.Controls.Add(CreateStatCard("Bugunku Ciro", "0.00 TL"));
        statsPanel.Controls.Add(CreateStatCard("Acik Masa Sayisi", "0"));
        statsPanel.Controls.Add(CreateStatCard("Bekleyen Paket", "0"));
        statsPanel.Controls.Add(CreateStatCard("Kart Toplami", "0.00 TL"));

        var recentOrdersGroup = UiFactory.CreateSection("Son Siparisler", new Point(0, 184), new Size(860, 310));
        recentOrdersGroup.Controls.Add(UiFactory.CreateEmptyState("Henuz servis tarafindan yuklenmis siparis yok."));

        var systemGroup = UiFactory.CreateSection("Sistem Durumu", new Point(884, 184), new Size(296, 310));
        systemGroup.Controls.Add(UiFactory.CreateEmptyState(
            "POS baglantisi, internet, yazici ve servis ozetleri bu alanda gercek zamanli gosterilecek.\r\n\r\nAktif uyarilar geldikce burada listelenecek."));

        Controls.Add(systemGroup);
        Controls.Add(recentOrdersGroup);
        Controls.Add(statsPanel);
    }

    private static Control CreateStatCard(string title, string value)
    {
        var panel = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 12, 0),
            Size = new Size(285, 82)
        };

        panel.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Location = new Point(14, 11),
            Text = title
        });

        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point, 162),
            Location = new Point(14, 34),
            Text = value
        });

        return panel;
    }
}
