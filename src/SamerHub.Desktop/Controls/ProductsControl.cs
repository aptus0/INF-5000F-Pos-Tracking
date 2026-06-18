namespace SamerHub.Desktop.Controls;

public sealed class ProductsControl : UserControl
{
    public ProductsControl()
    {
        BackColor = Color.WhiteSmoke;

        Controls.Add(UiFactory.CreateSubtitle(
            "Kategori, urun, fiyat ve ekstra secenek yonetimi bu ekranda olacak.",
            new Point(3, 34)));
        Controls.Add(UiFactory.CreateTitle("Urunler", new Point(0, 0)));

        var categories = UiFactory.CreateSection("Kategoriler", new Point(0, 72), new Size(320, 420));
        categories.Controls.Add(UiFactory.CreateEmptyState("Tanimli kategori bulunmuyor."));

        var products = UiFactory.CreateSection("Urun Listesi", new Point(350, 72), new Size(830, 420));
        products.Controls.Add(UiFactory.CreateEmptyState("Tanimli urun bulunmuyor. Ilk urun ve menuler burada listelenecek."));

        Controls.Add(categories);
        Controls.Add(products);
    }
}
