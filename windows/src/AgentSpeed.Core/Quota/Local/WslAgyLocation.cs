using AgentSpeed.Core.Locations;

namespace AgentSpeed.Core.Quota.Local;

/// <summary>The Linux account of an explicitly added Antigravity CLI data folder.</summary>
public sealed record WslAgyLocation
{
    private WslAgyLocation(string distribution, string user, string home)
    {
        Distribution = distribution;
        User = user;
        Home = home;
    }

    public string Distribution { get; }
    public string User { get; }
    private string Home { get; }

    public static WslAgyLocation? FromLocations(IEnumerable<DataLocation> locations) =>
        locations.Select(location => Parse(location.Path)).FirstOrDefault(location => location is not null);

    private static WslAgyLocation? Parse(string? path)
    {
        if (path is null || !path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return null;
        }

        var parts = path[2..].TrimEnd('\\').Split('\\');
        if (parts.Length < 5 || !(parts[0].Equals("wsl.localhost", StringComparison.OrdinalIgnoreCase)
            || parts[0].Equals("wsl$", StringComparison.OrdinalIgnoreCase)) || !IsDistribution(parts[1]))
        {
            return null;
        }

        var root = parts[2] == "root";
        var homeEnd = root ? 3 : 4;
        if (parts.Length < homeEnd + 2 || !(root || parts[2] == "home" && IsUser(parts[3]))
            || parts[homeEnd] != ".gemini" || parts[homeEnd + 1] != "antigravity-cli"
            || !(parts.Length == homeEnd + 2 || parts.Length == homeEnd + 3 && parts[homeEnd + 2] == "conversations"))
        {
            return null;
        }

        return root ? new(parts[1], "root", "/root") : new(parts[1], parts[3], $"/home/{parts[3]}");
    }

    private static bool IsDistribution(string value) => value.Length is > 0 and <= 128 && char.IsAsciiLetterOrDigit(value[0])
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ' ');

    private static bool IsUser(string value) => value.Length is > 0 and <= 32 && value != "root"
        && (char.IsAsciiLetter(value[0]) || value[0] == '_')
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    internal IReadOnlyList<string> Arguments(string directory, string timeout, IReadOnlyList<string> commandArguments) =>
    [
        "--distribution", Distribution, "--user", User, "--cd", directory, "--exec",
        "/usr/bin/timeout", "--kill-after=2s", timeout,
        "/usr/bin/env", "PATH=/dev/null", "BROWSER=/bin/false", "AGY_CLI_DISABLE_AUTO_UPDATE=true",
        $"{Home}/.local/bin/agy", .. commandArguments,
    ];
}
