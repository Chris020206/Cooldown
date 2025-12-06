namespace Cooldown.Service.Engine;

public interface IBlockingEngine
{
    Task PulseAsync(CancellationToken cancellationToken);
}
