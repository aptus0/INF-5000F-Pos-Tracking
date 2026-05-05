namespace SamerHub.Desktop.Controls;

public sealed class KitchenControl : UserControl
{
    public KitchenControl()
    {
        BackColor = Color.WhiteSmoke;

        Controls.Add(UiFactory.CreateSubtitle(
            "Mutfak ekraninda masa ve paket siparisleri hazirlama akisi takip edilecek.",
            new Point(3, 34)));
        Controls.Add(UiFactory.CreateTitle("Mutfak", new Point(0, 0)));

        var prep = UiFactory.CreateSection("Hazirlanacak Siparisler", new Point(0, 72), new Size(580, 420));
        prep.Controls.Add(UiFactory.CreateEmptyState("Hazirlanacak aktif siparis yok."));

        var completed = UiFactory.CreateSection("Hazirlananlar", new Point(600, 72), new Size(580, 420));
        completed.Controls.Add(UiFactory.CreateEmptyState("Hazirlanan siparis kaydi bulunmuyor."));

        Controls.Add(prep);
        Controls.Add(completed);
    }
}
