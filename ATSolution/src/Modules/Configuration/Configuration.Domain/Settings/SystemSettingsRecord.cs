using ATSolution.Domain.Entities.Common;

namespace Configuration.Domain.Settings;

public class SystemSettingsRecord : Entity<Guid>, IAuditableEntity
{
    public string SettingsJson { get; private set; } = "{}";
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static SystemSettingsRecord Create(string settingsJson)
    {
        var now = DateTimeOffset.UtcNow;
        return new SystemSettingsRecord
        {
            Id = Guid.NewGuid(),
            SettingsJson = settingsJson,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void UpdateSettings(string settingsJson)
    {
        SettingsJson = settingsJson;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
