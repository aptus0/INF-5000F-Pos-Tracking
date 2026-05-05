using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SamerHub.Core.Interfaces;
using SamerHub.Infrastructure.Persistence;

namespace SamerHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseSection = configuration.GetSection(DatabaseOptions.SectionName);
        var databaseOptions = new DatabaseOptions
        {
            Provider = databaseSection["Provider"] ?? "Sqlite",
            ConnectionString = databaseSection["ConnectionString"] ?? "Data Source=C:\\ProgramData\\SAMER Hub\\samerhub.db"
        };

        services.AddSingleton<IOptions<DatabaseOptions>>(Options.Create(databaseOptions));

        services.AddDbContext<SamerHubDbContext>((serviceProvider, options) =>
        {
            var resolvedOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var provider = resolvedOptions.Provider.Trim();

            if (provider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseMySql(
                    resolvedOptions.ConnectionString,
                    ServerVersion.AutoDetect(resolvedOptions.ConnectionString));
                return;
            }

            options.UseSqlite(resolvedOptions.ConnectionString);
        });

        services.AddScoped<JsonSettingsStore>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IPosCommandRepository, EfPosCommandRepository>();
        services.AddScoped<IPosSettingsRepository, EfPosSettingsRepository>();
        services.AddScoped<IPosTransactionRepository, EfPosTransactionRepository>();
        services.AddScoped<IPosBridgeStateRepository, EfPosBridgeStateRepository>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IFiscalReceiptRepository, EfFiscalReceiptRepository>();
        services.AddScoped<IFiscalSettingsRepository, EfFiscalSettingsRepository>();
        services.AddScoped<IPrinterSettingsRepository, EfPrinterSettingsRepository>();
        services.AddScoped<IPlatformSettingsRepository, EfPlatformSettingsRepository>();
        services.AddScoped<ITableRepository, EfTableRepository>();
        services.AddScoped<ITableSessionRepository, EfTableSessionRepository>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IDashboardQueryService, EfDashboardQueryService>();
        services.AddScoped<IInfrastructureHealthReporter, InfrastructureHealthReporter>();
        return services;
    }
}
