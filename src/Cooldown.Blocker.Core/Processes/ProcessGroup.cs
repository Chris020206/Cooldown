using System.Text.Json.Serialization;

namespace Cooldown.Blocker.Core.Processes;

/// <summary>
/// Represents a logical app/launcher grouping with primary processes and companion dependencies.
/// </summary>
public sealed class ProcessGroup
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("primary")]
    public List<string> PrimaryProcessNames { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public List<string> DependencyProcessNames { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Returns all process names (primary + dependencies) after normalization.
    /// </summary>
    public IReadOnlyList<string> AllProcessNames =>
        PrimaryProcessNames.Concat(DependencyProcessNames).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    internal void Normalize(Func<string, string> nameNormalizer)
    {
        PrimaryProcessNames = NormalizeList(PrimaryProcessNames, nameNormalizer);
        DependencyProcessNames = NormalizeList(DependencyProcessNames, nameNormalizer);

        if (string.IsNullOrWhiteSpace(Id) && PrimaryProcessNames.Count > 0)
        {
            Id = PrimaryProcessNames[0];
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = string.IsNullOrWhiteSpace(Id) ? "Unnamed Group" : Id;
        }
    }

    private static List<string> NormalizeList(IEnumerable<string> names, Func<string, string> nameNormalizer)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var raw in names ?? Array.Empty<string>())
        {
            var normalized = nameNormalizer(raw);
            if (string.IsNullOrWhiteSpace(normalized) || set.Contains(normalized))
            {
                continue;
            }

            set.Add(normalized);
            result.Add(normalized);
        }

        return result;
    }
}
