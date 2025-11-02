using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Cooldown.Blocker.Core;

public static class ProcessTerminator
{
    public static int TerminateExistingProcesses(IEnumerable<string> blockedProcessNames, Func<int, string, ProcessTerminationResult> terminator)
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
            return 0;
        }

        var terminated = 0;

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

        return terminated;
    }
}
