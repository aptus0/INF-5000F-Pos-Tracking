using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SamerHub.Infrastructure.Persistence;

public sealed class JsonSettingsStore(SamerHubDbContext dbContext)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<T> GetAsync<T>(string key, Func<T> createDefault, CancellationToken cancellationToken)
    {
        var record = await dbContext.AppSettings.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (record is null || string.IsNullOrWhiteSpace(record.JsonValue))
        {
            var defaultValue = createDefault();
            await SaveAsync(key, defaultValue, cancellationToken);
            return defaultValue;
        }

        return JsonSerializer.Deserialize<T>(record.JsonValue, SerializerOptions) ?? createDefault();
    }

    public async Task<T> SaveAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        var record = await dbContext.AppSettings.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (record is null)
        {
            record = new AppSettingRecord { Key = key };
            dbContext.AppSettings.Add(record);
        }

        record.JsonValue = JsonSerializer.Serialize(value, SerializerOptions);
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return value;
    }
}
