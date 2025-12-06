using System.ServiceProcess;
using Microsoft.Extensions.Hosting;

namespace Cooldown.Service.Hosting;

/// <summary>
/// Bridges the generic host to the classic Windows Service control manager so we can
/// keep the modern hosting model while running as a real service.
/// </summary>
public sealed class CooldownWindowsService : ServiceBase
{
    private readonly IHostBuilder _hostBuilder;
    private IHost? _host;

    public CooldownWindowsService(IHostBuilder hostBuilder)
    {
        _hostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));
        ServiceName = "Cooldown Service";
    }

    protected override void OnStart(string[] args)
    {
        _host = _hostBuilder.Build();
        _host.Start();
    }

    protected override void OnStop()
    {
        if (_host == null)
        {
            return;
        }

        _host.StopAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
        _host.Dispose();
        _host = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _host?.Dispose();
        }

        base.Dispose(disposing);
    }
}
