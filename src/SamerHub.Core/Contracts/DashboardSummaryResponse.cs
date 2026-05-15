namespace SamerHub.Core.Contracts;

public sealed class DashboardSummaryResponse
{
    public decimal TodayRevenue { get; set; }
    public decimal CashTotal { get; set; }
    public decimal CardTotal { get; set; }
    public decimal OnlineTotal { get; set; }
    public int OpenTables { get; set; }
    public int PendingPackageOrders { get; set; }
    public int KitchenInProgress { get; set; }
    public List<RecentOrderSummary> RecentOrders { get; set; } = [];
    public List<string> Alerts { get; set; } = [];
}

public sealed class RecentOrderSummary
{
    public string Reference { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
