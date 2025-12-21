using System;
using System.Collections.Generic;
using System.Linq;
using Cooldown.Blocker.Core;
using Xunit;

namespace Cooldown.Blocker.Core.Tests;

public class AppDependencyGraphTests
{
    [Fact]
    public void Expands_ToRequiredLauncher()
    {
        var graph = CreateGraph(
            new[]
            {
                new AppDefinition { Key = "League of Legends", ProcessNames = new List<string> { "LeagueClientUx", "LeagueClientUxRender" } },
                new AppDefinition { Key = "RiotClientServices", ProcessNames = new List<string> { "RiotClientServices" } }
            },
            new[]
            {
                new AppDependency { SourceKey = "League of Legends", TargetKey = "RiotClientServices" }
            });

        var result = graph.Expand(new[] { "League of Legends" });

        AssertContains("League of Legends", result.AppKeys);
        AssertContains("RiotClientServices", result.AppKeys);
        Assert.Contains(result.ProcessNames, p => string.Equals(p, "LeagueClientUx", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ProcessNames, p => string.Equals(p, "RiotClientServices", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Expands_TransitiveClosure()
    {
        var graph = CreateGraph(
            new[]
            {
                new AppDefinition { Key = "A", ProcessNames = new List<string> { "a" } },
                new AppDefinition { Key = "B", ProcessNames = new List<string> { "b" } },
                new AppDefinition { Key = "C", ProcessNames = new List<string> { "c" } }
            },
            new[]
            {
                new AppDependency { SourceKey = "A", TargetKey = "B" },
                new AppDependency { SourceKey = "B", TargetKey = "C" }
            });

        var result = graph.Expand(new[] { "A" });

        AssertContains("A", result.AppKeys);
        AssertContains("B", result.AppKeys);
        AssertContains("C", result.AppKeys);
    }

    [Fact]
    public void Deduplicates_SharedDependencies()
    {
        var graph = CreateGraph(
            new[]
            {
                new AppDefinition { Key = "A", ProcessNames = new List<string> { "a" } },
                new AppDefinition { Key = "B", ProcessNames = new List<string> { "b" } },
                new AppDefinition { Key = "C", ProcessNames = new List<string> { "c" } }
            },
            new[]
            {
                new AppDependency { SourceKey = "A", TargetKey = "C" },
                new AppDependency { SourceKey = "B", TargetKey = "C" }
            });

        var result = graph.Expand(new[] { "A", "B" });

        Assert.Equal(3, result.AppKeys.Count);
        Assert.Contains(result.ProcessNames, p => string.Equals(p, "c", StringComparison.OrdinalIgnoreCase));
        Assert.Single(result.ProcessNames.Where(p => string.Equals(p, "c", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Handles_CyclesWithWarning()
    {
        var logger = new ListLogger();
        var graph = CreateGraph(
            new[]
            {
                new AppDefinition { Key = "A", ProcessNames = new List<string> { "a" } },
                new AppDefinition { Key = "B", ProcessNames = new List<string> { "b" } }
            },
            new[]
            {
                new AppDependency { SourceKey = "A", TargetKey = "B" },
                new AppDependency { SourceKey = "B", TargetKey = "A" }
            });

        var result = graph.Expand(new[] { "A" }, logger: logger);

        AssertContains("A", result.AppKeys);
        AssertContains("B", result.AppKeys);
        Assert.NotEmpty(logger.Messages);
    }

    private static AppDependencyGraph CreateGraph(IEnumerable<AppDefinition> definitions, IEnumerable<AppDependency> dependencies) =>
        new(definitions, dependencies);

    private static void AssertContains(string expected, IReadOnlyCollection<string> collection)
    {
        Assert.Contains(collection, item => string.Equals(item, expected, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class ListLogger : Microsoft.Extensions.Logging.ILogger
    {
        public List<string> Messages { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
