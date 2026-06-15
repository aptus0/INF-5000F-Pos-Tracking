namespace SamerHub.Desktop.Controls;

public sealed class CashierControl : UserControl
{
    public CashierControl()
    {
        BackColor = Color.WhiteSmoke;

        Controls.Add(UiFactory.CreateSubtitle(
            "Nakit, kart, parcali odeme, iade ve gun sonu operasyonlari bu ekranda yonetilecek.",
            new Point(3, 34)));
        Controls.Add(UiFactory.CreateTitle("Kasa", new Point(0, 0)));

        var left = UiFactory.CreateSection("Bekleyen Odemeler", new Point(0, 72), new Size(570, 420));
        left.Controls.Add(UiFactory.CreateEmptyState("Odeme bekleyen masa veya paket siparisi yok."));

        var right = UiFactory.CreateSection("Gun Sonu ve Tahsilat Ozeti", new Point(610, 72), new Size(570, 420));
        right.Controls.Add(UiFactory.CreateEmptyState(
            "Gercek tahsilat hareketleri geldikce nakit, kart ve online odeme ozetleri bu alanda gosterilecek."));

        Controls.Add(left);
        Controls.Add(right);
    }
}
