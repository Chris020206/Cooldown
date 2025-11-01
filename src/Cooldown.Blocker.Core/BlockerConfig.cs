using System.Text.Json.Serialization;

namespace Cooldown.Blocker.Core;

public class BlockerConfig
{
    [JsonPropertyName("apps")]
    public List<BlockableApp> Apps { get; set; } = new();

    [JsonPropertyName("blockedProcessNames")]
    public List<string>? LegacyBlockedProcessNames { get; set; }

    [JsonPropertyName("checkIntervalMs")]
    public int CheckIntervalMs { get; set; } = 1000;

    [JsonPropertyName("enableToastNotifications")]
    public bool EnableToastNotifications { get; set; } = true;

    public IEnumerable<string> EnabledProcessNames => Apps
        .Where(app => app.Enabled)
        .Select(app => NormalizeProcessName(app.Name))
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Distinct(StringComparer.OrdinalIgnoreCase);

    public void Normalize()
    {
        if ((Apps == null || Apps.Count == 0) && LegacyBlockedProcessNames is { Count: > 0 })
        {
            Apps = LegacyBlockedProcessNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => new BlockableApp { Name = name.Trim(), Enabled = true })
                .ToList();
            LegacyBlockedProcessNames = null;
        }

        Apps ??= new List<BlockableApp>();

        foreach (var app in Apps)
        {
            if (string.IsNullOrWhiteSpace(app.Name))
            {
                app.Name = string.Empty;
                continue;
            }

            app.Name = NormalizeProcessName(app.Name);
        }
    }

    public static BlockerConfig CreateDefault()
    {
        return new BlockerConfig
        {
            Apps = new List<BlockableApp>
            {
                new("League of Legends"),
                new("RiotClientServices"),
                new("VALORANT"),
                new("steam"),
                new("steamwebhelper"),
                new("EpicGamesLauncher"),
                new("Battle.net"),
                new("Overwatch"),
                new("FortniteClient-Win64-Shipping"),
                new("csgo"),
                new("dota2")
            },
            CheckIntervalMs = 1000,
            EnableToastNotifications = true
        };
    }

    private static string NormalizeProcessName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }
}

public class BlockableApp
{
    public BlockableApp()
    {
    }

    public BlockableApp(string name, bool enabled = true)
    {
        Name = name;
        Enabled = enabled;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}
