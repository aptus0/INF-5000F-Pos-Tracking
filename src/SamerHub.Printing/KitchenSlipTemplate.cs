using SamerHub.Core.Entities;

namespace SamerHub.Printing;

public static class KitchenSlipTemplate
{
    public static string Build(Order order)
    {
        var lines = new List<string>
        {
            "SAMER HUB",
            $"Platform: {order.PlatformName}",
            $"Musteri: {order.CustomerName}",
            $"Odeme: {order.PaymentType}",
            "------------------------"
        };

        lines.AddRange(order.Items.Select(x => $"{x.Quantity}x {x.ProductName}"));
        lines.Add("------------------------");
        lines.Add($"Toplam: {order.TotalAmount:0.00} TRY");

        return string.Join(Environment.NewLine, lines);
    }
}
