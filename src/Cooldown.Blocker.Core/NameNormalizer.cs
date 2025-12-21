namespace Cooldown.Blocker.Core;

public static class NameNormalizer
{
    public static string NormalizeAppKey(string key) => NormalizeProcessName(key);

    public static string NormalizeProcessName(string name)
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
