using System.Diagnostics;

namespace Cooldown.Blocker.Core;

public static class ProcessTerminator
{
    public static ProcessTerminationSummary TerminateExistingProcesses(IEnumerable<string> blockedProcessNames, Func<int, string, ProcessTerminationResult> terminator)
    {
        if (blockedProcessNames == null)
        {
            throw new ArgumentNullException(nameof(blockedProcessNames));
        }

        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        var blocked = new HashSet<string>(blockedProcessNames.Where(name => !string.IsNullOrWhiteSpace(name)), StringComparer.OrdinalIgnoreCase);
        if (blocked.Count == 0)
        {
            return new ProcessTerminationSummary(0, Array.Empty<string>());
        }

        var terminated = 0;
        var terminatedNames = new List<string>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (!blocked.Contains(process.ProcessName))
                {
                    continue;
                }

                var result = terminator(process.Id, process.ProcessName);
                if (result.Status == ProcessTerminationStatus.Terminated)
                {
                    terminated++;
                    terminatedNames.Add(process.ProcessName);
                }
            }
            catch
            {
                // Ignore failures so one bad process does not block enforcement.
            }
            finally
            {
                process.Dispose();
            }
        }

        return new ProcessTerminationSummary(terminated, terminatedNames);
    }
}
