using Configuration.Application.Settings;

namespace Configuration.Application.Abstractions;

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsCommand command, CancellationToken cancellationToken = default);
}
