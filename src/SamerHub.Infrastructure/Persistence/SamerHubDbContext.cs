using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;

namespace SamerHub.Infrastructure.Persistence;

public sealed class SamerHubDbContext(DbContextOptions<SamerHubDbContext> options) : DbContext(options)
{
    public DbSet<AppSettingRecord> AppSettings => Set<AppSettingRecord>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PosCommand> PosCommands => Set<PosCommand>();
    public DbSet<PosTransaction> PosTransactions => Set<PosTransaction>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<VatRate> VatRates => Set<VatRate>();
    public DbSet<FiscalDepartment> FiscalDepartments => Set<FiscalDepartment>();
    public DbSet<FiscalReceipt> FiscalReceipts => Set<FiscalReceipt>();
    public DbSet<FiscalReceiptItem> FiscalReceiptItems => Set<FiscalReceiptItem>();
    public DbSet<FiscalPayment> FiscalPayments => Set<FiscalPayment>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<TableSession> TableSessions => Set<TableSession>();
    public DbSet<TableOrderItem> TableOrderItems => Set<TableOrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettingRecord>(entity =>
        {
            entity.ToTable("app_settings");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(128);
            entity.Property(x => x.JsonValue).HasColumnType("TEXT");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalOrderId).HasMaxLength(64);
            entity.Property(x => x.PlatformName).HasMaxLength(64);
            entity.Property(x => x.CustomerName).HasMaxLength(128);
            entity.Property(x => x.CustomerAddress).HasMaxLength(256);
            entity.Property(x => x.TotalAmount).HasColumnType("TEXT");
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductName).HasMaxLength(128);
            entity.Property(x => x.Note).HasMaxLength(256);
            entity.Property(x => x.UnitPrice).HasColumnType("TEXT");
        });

        modelBuilder.Entity<PosCommand>(entity =>
        {
            entity.ToTable("payment_commands");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(64);
            entity.Property(x => x.TerminalId).HasMaxLength(64);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.Amount).HasColumnType("TEXT");
            entity.Property(x => x.ResultCode).HasMaxLength(32);
            entity.Property(x => x.ResultMessage).HasMaxLength(128);
        });

        modelBuilder.Entity<PosTransaction>(entity =>
        {
            entity.ToTable("pos_transactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(64);
            entity.Property(x => x.TerminalId).HasMaxLength(64);
            entity.Property(x => x.TransactionId).HasMaxLength(64);
            entity.Property(x => x.Stan).HasMaxLength(64);
            entity.Property(x => x.ReceiptNumber).HasMaxLength(64);
            entity.Property(x => x.ApprovalCode).HasMaxLength(64);
            entity.Property(x => x.ErrorCode).HasMaxLength(32);
            entity.Property(x => x.ErrorMessage).HasMaxLength(128);
        });

        modelBuilder.Entity<VatRate>(entity =>
        {
            entity.ToTable("vat_rates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Rate).HasColumnType("TEXT");
            entity.Property(x => x.Label).HasMaxLength(128);
        });

        modelBuilder.Entity<FiscalDepartment>(entity =>
        {
            entity.ToTable("fiscal_departments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.PosCode).HasMaxLength(8);
            entity.Property(x => x.DefaultVatRate).HasColumnType("TEXT");
            entity.Property(x => x.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.Barcode).HasMaxLength(64);
            entity.Property(x => x.CategoryName).HasMaxLength(128);
            entity.Property(x => x.UnitPrice).HasColumnType("TEXT");
            entity.Property(x => x.KdvOrani).HasColumnType("TEXT");
            entity.Property(x => x.YnokcDepartmani).HasMaxLength(16);
            entity.Property(x => x.UnitType).HasMaxLength(32);
            entity.Property(x => x.MaliUrunKodu).HasMaxLength(64);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<FiscalReceipt>(entity =>
        {
            entity.ToTable("fiscal_receipts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(64);
            entity.Property(x => x.ReceiptNo).HasMaxLength(64);
            entity.Property(x => x.ZNo).HasMaxLength(64);
            entity.Property(x => x.EcrNo).HasMaxLength(64);
            entity.Property(x => x.TotalAmount).HasColumnType("TEXT");
            entity.Property(x => x.TotalVat).HasColumnType("TEXT");
            entity.Property(x => x.RawResponse).HasMaxLength(4096);
            entity.Property(x => x.ErrorCode).HasMaxLength(32);
            entity.Property(x => x.ErrorMessage).HasMaxLength(256);
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.FiscalReceiptId);
            entity.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.FiscalReceiptId);
        });

        modelBuilder.Entity<FiscalReceiptItem>(entity =>
        {
            entity.ToTable("fiscal_receipt_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductName).HasMaxLength(256);
            entity.Property(x => x.UnitPrice).HasColumnType("TEXT");
            entity.Property(x => x.LineTotal).HasColumnType("TEXT");
            entity.Property(x => x.VatRate).HasColumnType("TEXT");
            entity.Property(x => x.DepartmentCode).HasMaxLength(8);
        });

        modelBuilder.Entity<FiscalPayment>(entity =>
        {
            entity.ToTable("fiscal_payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PaymentCode).HasMaxLength(32);
            entity.Property(x => x.Amount).HasColumnType("TEXT");
        });

        modelBuilder.Entity<DiningTable>(entity =>
        {
            entity.ToTable("dining_tables");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(64);
            entity.Property(x => x.Area).HasMaxLength(64);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.ActiveOrderTotal).HasColumnType("TEXT");
        });

        modelBuilder.Entity<TableSession>(entity =>
        {
            entity.ToTable("table_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TableName).HasMaxLength(64);
            entity.Property(x => x.TotalAmount).HasColumnType("TEXT");
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.TableSessionId);
            entity.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.TableSessionId);
        });

        modelBuilder.Entity<TableOrderItem>(entity =>
        {
            entity.ToTable("table_order_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductName).HasMaxLength(256);
            entity.Property(x => x.CategoryName).HasMaxLength(128);
            entity.Property(x => x.UnitPrice).HasColumnType("TEXT");
            entity.Property(x => x.VatRate).HasColumnType("TEXT");
            entity.Property(x => x.DepartmentCode).HasMaxLength(16);
            entity.Property(x => x.Note).HasMaxLength(256);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("TEXT");
            entity.Property(x => x.Reference).HasMaxLength(128);
        });
    }
}
