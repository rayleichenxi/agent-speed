using AgentSpeed.Core.Locations;
using AgentSpeed.Core.Quota.Local;

namespace AgentSpeed.Core.Tests.Quota.Local;

public sealed class WslAgyLocationTests
{
    [Theory]
    [InlineData(@"\\wsl.localhost\Ubuntu-22.04\root\.gemini\antigravity-cli\conversations", "Ubuntu-22.04", "root", "/root")]
    [InlineData(@"\\wsl$\Ubuntu Custom\home\dev-user\.gemini\antigravity-cli", "Ubuntu Custom", "dev-user", "/home/dev-user")]
    public void UsesTheConfiguredLinuxAccount(string path, string distribution, string user, string home)
    {
        var location = WslAgyLocation.FromLocations([DataLocation.Automatic, DataLocation.At(path)]);
        Assert.NotNull(location);
        Assert.Equal(distribution, location.Distribution);
        Assert.Equal(user, location.User);
        Assert.Equal(
            ["--distribution", distribution, "--user", user, "--cd", @"C:\Temp\Private Folder", "--exec",
                "/usr/bin/timeout", "--kill-after=2s", "10s", "/usr/bin/env",
                "PATH=/dev/null", "BROWSER=/bin/false", "AGY_CLI_DISABLE_AUTO_UPDATE=true", $"{home}/.local/bin/agy", "--version"],
            location.Arguments(@"C:\Temp\Private Folder", "10s", ["--version"]));
    }

    [Theory]
    [InlineData(@"\\fileserver\Ubuntu\root\.gemini\antigravity-cli")]
    [InlineData(@"\\wsl.localhost\--system\root\.gemini\antigravity-cli")]
    [InlineData(@"\\wsl.localhost\Ubuntu\home\..\root\.gemini\antigravity-cli")]
    [InlineData(@"\\wsl.localhost\Ubuntu\home\-user\.gemini\antigravity-cli")]
    [InlineData(@"\\wsl.localhost\Ubuntu\root\.gemini\antigravity")]
    [InlineData(@"\\wsl.localhost\Ubuntu\root\.gemini\antigravity-cli\conversations\backup")]
    public void IgnoresPathsOutsideTheCliStore(string path) =>
        Assert.Null(WslAgyLocation.FromLocations([DataLocation.At(path)]));
}
