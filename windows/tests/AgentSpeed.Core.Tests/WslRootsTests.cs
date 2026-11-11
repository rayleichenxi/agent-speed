using System.Diagnostics;
using System.Text;
using AgentSpeed.Core.Monitoring;
using AgentSpeed.Core.Locations;

namespace AgentSpeed.Core.Tests;

public sealed class WslRootsTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void AdditionalRootsAreKeptAlongsideLocalAndIdenticalRootsAreNotRepeated()
    {
        var localCodex = _temp.Combine("local", "codex", "sessions");
        var localClaude = _temp.Combine("local", "claude", "projects");
        var extraCodex = _temp.Combine("wsl", "codex", "sessions");
        var extraClaude = _temp.Combine("wsl", "claude", "projects");
        var configuration = Configuration(localCodex, localClaude, extraCodex, extraClaude);
        var locations = SourceLocations.Resolve(configuration);
        Assert.Equal(new[] { localCodex, extraCodex }, locations.FileRoots().Where(x => x.Family == RootFamily.Codex).Select(x => x.Path));
        Assert.Equal(new[] { localClaude, extraClaude }, locations.FileRoots().Where(x => x.Family == RootFamily.Claude).Select(x => x.Path));
        var duplicate = SourceLocations.Resolve(Configuration(localCodex, localClaude, localCodex, localClaude));
        Assert.Single(duplicate.FileRoots(), x => x.Family == RootFamily.Codex);
        Assert.Single(duplicate.FileRoots(), x => x.Family == RootFamily.Claude);
        Assert.NotEqual(configuration, Configuration(localCodex, localClaude, extraCodex, localClaude));
    }

    [Fact]
    public void PollAndRediscoveryReadBothAdditionalRoots()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var start = now - 60_000;
        var localCodex = _temp.Combine("local", "codex", "sessions");
        var localClaude = _temp.Combine("local", "claude", "projects");
        var extraCodex = _temp.Combine("wsl", "codex", "sessions");
        var extraClaude = _temp.Combine("wsl", "claude", "projects");
        foreach (var root in new[] { localCodex, localClaude, extraCodex, extraClaude }) Directory.CreateDirectory(root);
        var configuration = Configuration(localCodex, localClaude, extraCodex, extraClaude);
        using var core = new MonitorCore(configuration, SourceLocations.Resolve(configuration), start);
        core.Seed(now, start, CancellationToken.None);
        var codex = Path.Combine(extraCodex, "rollout-test.jsonl");
        File.WriteAllText(codex, string.Join('\n', new[] {
            CodexLines.SessionMeta(now - 5000, "wsl-thread", "/tmp/project"), CodexLines.TurnContext(now - 4900),
            CodexLines.TaskStarted(now - 4000), CodexLines.UsageRecord(now - 1000, "wsl-response", CodexLines.Usage(100, 0, 40, 0), "wsl-thread") }) + "\n");
        var claude = Path.Combine(extraClaude, "test.jsonl");
        File.WriteAllText(claude, ClaudeLines.UserText(now - 4000, "fixture") + "\n");
        Assert.True(core.Rediscover(now, start, CancellationToken.None));
        File.AppendAllText(claude, ClaudeLines.Assistant(now - 1000, "wsl-claude-response", 30) + "\n");
        Assert.True(core.PollRecent(now));
        var (snapshot, _) = core.BuildSnapshot(now, start, MonitorState.Running, observeLive: true);
        Assert.Equal(70, snapshot.Today.Output);
        Assert.Equal(2, snapshot.Today.Requests);
    }

    [Fact]
    public void WindowsMonitorReadsLinuxWritesThroughWslShare()
    {
        var share = Environment.GetEnvironmentVariable("AGENTSPEED_TEST_WSL_SHARE");
        var linux = Environment.GetEnvironmentVariable("AGENTSPEED_TEST_WSL_LINUX_ROOT");
        if (!OperatingSystem.IsWindows() || share is null || linux is null) return;
        Assert.StartsWith("/tmp/agentspeed-fixture-", linux, StringComparison.Ordinal);
        Assert.StartsWith(@"\\wsl.localhost\Ubuntu-22.04\tmp\agentspeed-fixture-", share, StringComparison.OrdinalIgnoreCase);
        var codexRoot = Path.Combine(share, "codex", "sessions");
        var claudeRoot = Path.Combine(share, "claude", "projects");
        Directory.CreateDirectory(codexRoot);
        Directory.CreateDirectory(claudeRoot);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var start = now - 60_000;
        var configuration = Configuration(_temp.Combine("local-codex"), _temp.Combine("local-claude"), codexRoot, claudeRoot);
        using var core = new MonitorCore(configuration, SourceLocations.Resolve(configuration), start);
        core.Seed(now, start, CancellationToken.None);
        WriteLinux(linux + "/codex/sessions/rollout-test.jsonl", string.Join('\n', new[] {
            CodexLines.SessionMeta(now - 5000, "linux-thread", "/tmp/project"), CodexLines.TurnContext(now - 4900),
            CodexLines.TaskStarted(now - 4000), CodexLines.UsageRecord(now - 1000, "linux-response", CodexLines.Usage(100, 0, 40, 0), "linux-thread") }) + "\n");
        WriteLinux(linux + "/claude/projects/test.jsonl", ClaudeLines.UserText(now - 4000, "fixture") + "\n");
        Assert.True(core.Rediscover(now, start, CancellationToken.None));
        WriteLinux(linux + "/claude/projects/test.jsonl", ClaudeLines.Assistant(now - 1000, "linux-claude-response", 30) + "\n");
        Assert.True(core.PollRecent(now));
        var (snapshot, _) = core.BuildSnapshot(now, start, MonitorState.Running, observeLive: true);
        Assert.Equal(70, snapshot.Today.Output);
        Assert.Equal(2, snapshot.Today.Requests);
    }

    private static MonitorConfiguration Configuration(string codex, string claude, string extraCodex, string extraClaude) =>
        new(new HashSet<SourceKind> { SourceKind.Codex, SourceKind.ClaudeCode })
        {
            Locations = LocationLists.Automatic
                .With(LocationFolder.Codex, [DataLocation.At(codex), DataLocation.At(extraCodex)])
                .With(LocationFolder.Claude, [DataLocation.At(claude), DataLocation.At(extraClaude)]),
        };

    private static void WriteLinux(string path, string text)
    {
        var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "wsl.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in new[] { "-d", "Ubuntu-22.04", "--exec", "python3", "-c",
            "import base64,sys; open(sys.argv[1], 'ab').write(base64.b64decode(sys.argv[2]))", path,
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text)) }) info.ArgumentList.Add(arg);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start WSL fixture writer.");
        Assert.True(process.WaitForExit(15_000));
        Assert.Equal(0, process.ExitCode);
    }
}
