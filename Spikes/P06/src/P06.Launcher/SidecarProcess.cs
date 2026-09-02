using System.Diagnostics;

namespace CRNL.HiBoP.Spikes.P06.Launcher;

internal sealed class SidecarProcess : IDisposable
{
    private readonly Process process;

    private SidecarProcess(Process process)
    {
        this.process = process;
    }

    public int ProcessId => process.Id;

    public static SidecarProcess Start(string executable, IReadOnlyList<string> arguments, string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        var startInfo = CreateStartInfo(executable, arguments);
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The P06 host process could not be started.");
        _ = CopyOutputAsync(process.StandardOutput, Path.Combine(logDirectory, "host.stdout.log"));
        _ = CopyOutputAsync(process.StandardError, Path.Combine(logDirectory, "host.stderr.log"));
        return new SidecarProcess(process);
    }

    internal static ProcessStartInfo CreateStartInfo(string executable, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    public async Task<int> WaitForExitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            return 0;
        }

        return process.ExitCode;
    }

    public void Dispose()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }

        process.Dispose();
    }

    private static async Task CopyOutputAsync(StreamReader reader, string path)
    {
        await using var writer = new StreamWriter(path, append: false);
        while (await reader.ReadLineAsync() is { } line)
        {
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
        }
    }
}
