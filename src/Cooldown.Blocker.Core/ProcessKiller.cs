using System.Diagnostics;
using System.Management;

namespace Cooldown.Blocker.Core;

public static class ProcessKiller
{
    public static ProcessTerminationResult TerminateProcess(int pid, string processName)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
            return ProcessTerminationResult.Terminated(processName);
        }
        catch (ArgumentException)
        {
            return ProcessTerminationResult.AlreadyExited(processName);
        }
        catch (InvalidOperationException ex)
        {
            return ProcessTerminationResult.Failed(processName, ex.Message);
        }
        catch (Exception ex)
        {
            var wmiResult = TerminateViaWmi(pid, processName);
            return wmiResult ?? ProcessTerminationResult.Failed(processName, ex.Message);
        }
    }

    private static ProcessTerminationResult? TerminateViaWmi(int pid, string processName)
    {
        try
        {
            var query = $"SELECT * FROM Win32_Process WHERE ProcessId = {pid}";
            using var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject process in searcher.Get())
            {
                process.InvokeMethod("Terminate", null);
                process.Dispose();
                return ProcessTerminationResult.Terminated(processName);
            }

            return null;
        }
        catch (ManagementException ex)
        {
            return ProcessTerminationResult.Failed(processName, ex.Message);
        }
    }
}
