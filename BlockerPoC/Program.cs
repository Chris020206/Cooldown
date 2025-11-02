using System.Text.Json;
using Cooldown.Blocker.Core;

namespace Cooldown.BlockerPoC;

internal static class Program
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static BlockerEngine? _engine;
    private static BlockerConfig? _config;
    private static bool _isRunning = true;

    private static async Task Main()
    {
        Console.WriteLine("=== Cooldown.gg Process Blocker PoC ===\n");

        _config = await LoadConfigAsync();
        _engine = new BlockerEngine(_config);
        _engine.ProcessBlocked += OnProcessBlocked;
        _engine.LockStateChanged += (_, args) =>
        {
            if (!args.State.IsActive)
            {
                Console.WriteLine("\n🔓 Lock expired\n");
            }
        };

        await _engine.StartAsync();
        PrintMonitoredProcesses();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            _isRunning = false;
        };

        while (_isRunning)
        {
            RenderMenu();
            var input = Console.ReadLine();
            if (input == null)
            {
                break;
            }

            switch (input)
            {
                case "1":
                    CreateLock(5, LockType.Soft);
                    break;
                case "2":
                    CreateLock(60, LockType.Hard);
                    break;
                case "3":
                    PrintStatus();
                    break;
                case "4":
                    CancelLock();
                    break;
                case "5":
                    _isRunning = false;
                    break;
                default:
                    Console.WriteLine("Unknown option. Try again.");
                    break;
            }
        }

        if (_engine != null)
        {
            await _engine.DisposeAsync();
        }

        Console.WriteLine("\nShutdown complete.");
    }

    private static async Task<BlockerConfig> LoadConfigAsync()
    {
        const string fileName = "blocker-config.json";

        if (!File.Exists(fileName))
        {
            var defaultConfig = BlockerConfig.CreateDefault();
            await using var createStream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(createStream, defaultConfig, SerializerOptions);
            return defaultConfig;
        }

        await using var stream = File.OpenRead(fileName);
        var config = await JsonSerializer.DeserializeAsync<BlockerConfig>(stream, SerializerOptions) ?? BlockerConfig.CreateDefault();
        config.Normalize();
        return config;
    }

    private static void RenderMenu()
    {
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  [1] Lock for 5 minutes (soft)");
        Console.WriteLine("  [2] Lock for 1 hour (hard)");
        Console.WriteLine("  [3] Check lock status");
        Console.WriteLine("  [4] Cancel lock (soft only)");
        Console.WriteLine("  [5] Exit");
        Console.Write("\nChoice: ");
    }

    private static void PrintMonitoredProcesses()
    {
        if (_config == null)
        {
            return;
        }

        Console.WriteLine("Monitored processes:");
        foreach (var app in _config.Apps.Where(app => app.Enabled))
        {
            Console.WriteLine($"  - {app.Name}");
        }
    }

    private static void CreateLock(int minutes, LockType type)
    {
        if (_engine == null)
        {
            return;
        }

        var state = _engine.CreateLock(minutes, type);
        Console.WriteLine($"\n🔒 {type} lock created for {minutes} minutes");
        Console.WriteLine($"   Ends at: {state.EndTime:HH:mm:ss}");
    }

    private static void CancelLock()
    {
        if (_engine == null)
        {
            return;
        }

        if (_engine.CancelLock())
        {
            Console.WriteLine("\n✓ Lock canceled");
        }
        else
        {
            Console.WriteLine("\n✗ Cannot cancel (hard lock or no active lock)");
        }
    }

    private static void PrintStatus()
    {
        if (_engine == null)
        {
            return;
        }

        var status = _engine.GetStatus();
        if (status.IsActive)
        {
            var remaining = status.EndTime - DateTimeOffset.Now;
            Console.WriteLine($"\n🔒 Lock Status: ACTIVE ({status.Type})");
            Console.WriteLine($"   Time remaining: {Math.Max(0, (int)remaining.TotalMinutes)}m {Math.Max(0, remaining.Seconds)}s");
            Console.WriteLine($"   Ends at: {status.EndTime:HH:mm:ss}");
        }
        else
        {
            Console.WriteLine("\n✓ No active lock");
        }
    }

    private static void OnProcessBlocked(object? sender, ProcessBlockedEventArgs e)
    {
        var icon = e.Result.Status switch
        {
            ProcessTerminationStatus.Terminated => "✓",
            ProcessTerminationStatus.AlreadyExited => "ⓘ",
            _ => "✗"
        };

        Console.WriteLine($"\n⚠️  BLOCKED: {e.ProcessName} (PID: {e.ProcessId})");
        Console.WriteLine($"   {icon} {e.Result.Message}");
    }
}
