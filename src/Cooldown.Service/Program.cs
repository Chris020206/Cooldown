using System.ServiceProcess;
using Cooldown.Persistence;
using Cooldown.Service.Engine;
using Cooldown.Service.Hosting;
using Cooldown.Service.IPC;
using Cooldown.Service.Options;
using Cooldown.Service.State;
using Cooldown.Service.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;

namespace Cooldown.Service;

internal static class Program
{
    private const string ConsoleSwitch = "--console";

    /// <summary>
    /// Entry point that supports both Windows Service execution and console debugging.
    /// Pass --console to run in interactive mode (e.g. F5 in Visual Studio).
    /// This is a scaffold for Phase 2: hosting, DI, logging, and a heartbeat worker.
    /// </summary>
    public static async Task Main(string[] args)
    {
        var isConsole = args.Any(arg => string.Equals(arg, ConsoleSwitch, StringComparison.OrdinalIgnoreCase));
        var isWindowsService = !isConsole && WindowsServiceHelpers.IsWindowsService();

        var filteredArgs = FilterServiceArgs(args);
        var builder = CreateHostBuilder(filteredArgs, isWindowsService);

        if (isWindowsService)
        {
            ServiceBase.Run(new CooldownWindowsService(builder));
            return;
        }

        using var host = builder.Build();
        await InitializePersistenceAsync(host).ConfigureAwait(false);
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args, bool isWindowsService)
    {
        var builder = Host.CreateDefaultBuilder(args);

        if (isWindowsService)
        {
            // Windows Service integration keeps SCM happy and switches logging to Event Log below.
            builder = builder.UseWindowsService(options => options.ServiceName = "Cooldown Service")
                             .UseContentRoot(AppContext.BaseDirectory);
        }

        builder.ConfigureLogging((context, logging) =>
        {
            logging.ClearProviders();
            logging.AddConfiguration(context.Configuration.GetSection("Logging"));
            if (isWindowsService)
            {
                logging.AddEventLog();
            }
            else
            {
                logging.AddConsole();
                logging.AddDebug();
            }
        });

        builder.ConfigureServices((context, services) =>
        {
            services.Configure<ServiceOptions>(context.Configuration.GetSection("Service"));
            var dbPath = PersistencePaths.GetServiceDatabasePath();

            services.AddSingleton(sp => new SqliteDatabaseInitializer(dbPath, sp.GetService<ILogger<SqliteDatabaseInitializer>>()));
            services.AddSingleton<ILockStateRepository>(sp => new SqliteLockStateRepository(dbPath, sp.GetService<ILogger<SqliteLockStateRepository>>()));
            services.AddSingleton<ISettingsRepository>(sp => new SqliteSettingsRepository(dbPath, sp.GetService<ILogger<SqliteSettingsRepository>>()));

            services.AddSingleton<ILockStateManager, SqliteLockStateManager>();
            services.AddSingleton<IBlockingEngine, BlockingEngineStub>();
            services.AddSingleton<INamedPipeServer, NamedPipeServer>();

            services.AddHostedService<BlockingServiceWorker>();
        });

        return builder;
    }

    private static string[] FilterServiceArgs(string[] args) =>
        args.Where(arg => !string.Equals(arg, ConsoleSwitch, StringComparison.OrdinalIgnoreCase)).ToArray();

    private static async Task InitializePersistenceAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
        var initializer = services.GetRequiredService<SqliteDatabaseInitializer>();

        await initializer.InitDatabaseAsync().ConfigureAwait(false);

        var lockRepo = services.GetRequiredService<ILockStateRepository>();
        var lockStateManager = services.GetRequiredService<ILockStateManager>();
        var pipeServer = services.GetRequiredService<INamedPipeServer>();
        var current = await lockRepo.GetCurrentAsync().ConfigureAwait(false);
        if (current == null || !current.IsActive)
        {
            logger.LogInformation("SQLite persistence initialized; no lock state found.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var remaining = current.ExpiresAtUtc - now;
        if (remaining <= TimeSpan.Zero)
        {
            logger.LogInformation("Found expired lock in SQLite (expired at {ExpiresAt}); clearing.", current.ExpiresAtUtc);
            await lockRepo.ClearAsync().ConfigureAwait(false);
            return;
        }

        var parsedType = Enum.TryParse<LockType>(current.LockType, true, out var lockType)
            ? lockType
            : LockType.Soft;

        await lockStateManager.StartLockAsync(new LockParameters
        {
            Type = parsedType,
            Duration = remaining,
            BlockedApps = current.BlockedApps?.ToArray() ?? Array.Empty<string>()
        }).ConfigureAwait(false);

        logger.LogInformation("Rehydrated active {Type} lock from SQLite (remaining={Remaining}, blockedApps={BlockedApps}).",
            parsedType,
            remaining,
            current.BlockedApps?.Count ?? 0);

        // Optional: push a state change event so the desktop updates immediately.
        await pipeServer.BroadcastLockStateAsync("Rehydrated", CancellationToken.None).ConfigureAwait(false);
    }
}
