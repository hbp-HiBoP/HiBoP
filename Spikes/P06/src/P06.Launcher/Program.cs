using System.Globalization;
using System.Text.Json;
using CRNL.HiBoP.Spikes.P06.Launcher;

var options = LauncherOptions.Parse(args);
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.ShutdownAfterSeconds));
using var sidecar = SidecarProcess.Start(options.HostExecutable, options.HostArguments, options.LogDirectory);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.StateFile))!);
await File.WriteAllTextAsync(
    options.StateFile,
    JsonSerializer.Serialize(
        new
        {
            status = "started-hidden",
            launcherProcessId = Environment.ProcessId,
            sidecarProcessId = sidecar.ProcessId,
            createNoWindow = true,
            useShellExecute = false,
        }),
    timeout.Token);
Environment.ExitCode = await sidecar.WaitForExitAsync(timeout.Token);

internal sealed record LauncherOptions(
    string HostExecutable,
    IReadOnlyList<string> HostArguments,
    string LogDirectory,
    string StateFile,
    int ShutdownAfterSeconds)
{
    public static LauncherOptions Parse(string[] args)
    {
        var separator = Array.IndexOf(args, "--");
        var ownArguments = separator < 0 ? args : args[..separator];
        var hostArguments = separator < 0 ? Array.Empty<string>() : args[(separator + 1)..];
        var values = ownArguments
            .Chunk(2)
            .Where(pair => pair.Length == 2 && pair[0].StartsWith("--", StringComparison.Ordinal))
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.Ordinal);
        return new LauncherOptions(
            Path.GetFullPath(
                values.GetValueOrDefault("--host")
                    ?? throw new ArgumentException("--host is required.")),
            hostArguments,
            Path.GetFullPath(values.GetValueOrDefault("--log-directory", ".artifacts/launcher")),
            Path.GetFullPath(values.GetValueOrDefault("--state-file", ".artifacts/launcher/state.json")),
            int.Parse(values.GetValueOrDefault("--shutdown-after-seconds", "30"), CultureInfo.InvariantCulture));
    }
}
