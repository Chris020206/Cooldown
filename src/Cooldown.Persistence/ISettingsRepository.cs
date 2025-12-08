namespace Cooldown.Persistence;

public interface ISettingsRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SetValueAsync(string key, string valueJson, CancellationToken cancellationToken = default);
}
