using System.IO;
using System.Text.Json;
using Cooldown.Blocker.Core;

namespace Cooldown.Desktop.Services;

public class BlockerConfigService
{
    private const string ConfigFileName = "blocker-config.json";
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _configPath;

    public BlockerConfigService()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CooldownGG");
        Directory.CreateDirectory(root);
        _configPath = Path.Combine(root, ConfigFileName);
    }

    public async Task<BlockerConfig> LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            var defaultConfig = BlockerConfig.CreateDefault();
            await SaveAsync(defaultConfig);
            return defaultConfig;
        }

        await using var stream = File.OpenRead(_configPath);
        var config = await JsonSerializer.DeserializeAsync<BlockerConfig>(stream, _serializerOptions) ?? BlockerConfig.CreateDefault();
        config.Normalize();
        return config;
    }

    public async Task SaveAsync(BlockerConfig config)
    {
        config.Normalize();
        await using var stream = File.Create(_configPath);
        await JsonSerializer.SerializeAsync(stream, config, _serializerOptions);
    }
}
