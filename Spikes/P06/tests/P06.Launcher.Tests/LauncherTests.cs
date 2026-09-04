using System.Diagnostics;
using CRNL.HiBoP.Spikes.P06.Launcher;
using Xunit;

namespace CRNL.HiBoP.Spikes.P06.Launcher.Tests;

public sealed class LauncherTests
{
    [Fact]
    public void SidecarStartInfoIsInvisibleAndDoesNotUseShell()
    {
        var startInfo = SidecarProcess.CreateStartInfo("host.exe", ["--port", "5443"]);

        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.CreateNoWindow);
        Assert.Equal(ProcessWindowStyle.Hidden, startInfo.WindowStyle);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.Equal(["--port", "5443"], startInfo.ArgumentList);
    }
}
