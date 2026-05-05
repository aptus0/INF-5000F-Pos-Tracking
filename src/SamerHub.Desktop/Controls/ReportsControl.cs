namespace SamerHub.Desktop.Controls;

public sealed class ReportsControl : UserControl
{
    public ReportsControl()
    {
        BackColor = Color.WhiteSmoke;

        Controls.Add(UiFactory.CreateSubtitle(
            "Ciro, odeme dagilimi, platform satislari ve POS islem raporlari burada gosterilecek.",
            new Point(3, 34)));
        Controls.Add(UiFactory.CreateTitle("Raporlar", new Point(0, 0)));

        var left = UiFactory.CreateSection("Finansal Raporlar", new Point(0, 72), new Size(580, 420));
        left.Controls.Add(UiFactory.CreateEmptyState("Rapor verisi henuz olusmadi."));

        var right = UiFactory.CreateSection("Operasyon Raporlari", new Point(600, 72), new Size(580, 420));
        right.Controls.Add(UiFactory.CreateEmptyState("POS, iptal, iade ve urun bazli raporlar burada listelenecek."));

        Controls.Add(left);
        Controls.Add(right);
    }
}
