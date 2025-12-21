namespace Cooldown.Blocker.Core;

/// <summary>
/// Describes a logical app and the concrete process names that represent it.
/// This enables mapping friendly titles (e.g., "League of Legends") to one or more executables.
/// </summary>
public sealed class AppDefinition
{
    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<string> ProcessNames { get; set; } = new();

    public void Normalize(Func<string, string> nameNormalizer)
    {
        Key = nameNormalizer(Key);
        DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? Key : DisplayName.Trim();
        ProcessNames = ProcessNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(nameNormalizer)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
