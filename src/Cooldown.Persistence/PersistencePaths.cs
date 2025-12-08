namespace Cooldown.Persistence;

public static class PersistencePaths
{
    public static string GetServiceDatabasePath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dir = Path.Combine(baseDir, "CooldownGG");
        return Path.Combine(dir, "service.db");
    }
}
