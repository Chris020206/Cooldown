using System.ServiceProcess;
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

            services.AddSingleton<ILockStateManager, InMemoryLockStateManager>();
            services.AddSingleton<IBlockingEngine, BlockingEngineStub>();
            services.AddSingleton<INamedPipeServer, NamedPipeServer>();

            services.AddHostedService<BlockingServiceWorker>();
        });

        return builder;
    }

    private static string[] FilterServiceArgs(string[] args) =>
        args.Where(arg => !string.Equals(arg, ConsoleSwitch, StringComparison.OrdinalIgnoreCase)).ToArray();
}
