using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BluetoothMonitor.App.Services;

public interface IUpdateChecker
{
    Task<UpdateCheckResult> CheckForUpdateAsync(
        string currentVersion,
        CancellationToken cancellationToken = default
    );
}

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    string? LatestVersion = null,
    Uri? ReleaseUrl = null
);

public sealed class GitHubUpdateChecker(HttpClient httpClient) : IUpdateChecker
{
    private static readonly string LatestReleaseApi = AppResources.Get("Url.LatestReleaseApi");

    public async Task<UpdateCheckResult> CheckForUpdateAsync(
        string currentVersion,
        CancellationToken cancellationToken = default
    )
    {
        var release = await httpClient.GetFromJsonAsync<GitHubRelease>(
            LatestReleaseApi,
            cancellationToken
        );
        var latestText = release?.Name;
        if (!SemanticVersion.TryParse(latestText, out var latest))
        {
            latestText = release?.TagName;
        }

        if (latestText is null || !SemanticVersion.TryParse(latestText, out latest))
            return new(false);

        return new(
            latest.CompareTo(SemanticVersion.Parse(currentVersion)) > 0,
            latestText,
            Uri.TryCreate(release!.HtmlUrl, UriKind.Absolute, out var url) ? url : null
        );
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("html_url")] string? HtmlUrl
    );
}

internal readonly record struct SemanticVersion(int Major, int Minor, int Patch, string? PreRelease)
    : IComparable<SemanticVersion>
{
    public static SemanticVersion Parse(string value) =>
        TryParse(value, out var result)
            ? result
            : throw new FormatException($"Invalid semantic version: '{value}'.");

    public static bool TryParse(string? value, out SemanticVersion result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var parts = value.Trim().TrimStart('v', 'V').Split('-', 2);
        var numbers = parts[0].Split('.');
        if (
            numbers.Length != 3
            || !int.TryParse(numbers[0], out var major)
            || !int.TryParse(numbers[1], out var minor)
            || !int.TryParse(numbers[2].Split('+')[0], out var patch)
        )
            return false;
        result = new(major, minor, patch, parts.Length == 2 ? parts[1].Split('+')[0] : null);
        return true;
    }

    public int CompareTo(SemanticVersion other)
    {
        var result = Major.CompareTo(other.Major);
        if (result != 0)
            return result;
        result = Minor.CompareTo(other.Minor);
        if (result != 0)
            return result;
        result = Patch.CompareTo(other.Patch);
        if (result != 0)
            return result;
        if (PreRelease is null)
            return other.PreRelease is null ? 0 : 1;
        if (other.PreRelease is null)
            return -1;
        var left = PreRelease.Split('.');
        var right = other.PreRelease.Split('.');
        for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
        {
            if (i >= left.Length)
                return -1;
            if (i >= right.Length)
                return 1;
            var ln = int.TryParse(left[i], out var l);
            var rn = int.TryParse(right[i], out var r);
            if (ln && rn)
            {
                result = l.CompareTo(r);
            }
            else if (ln != rn)
                return ln ? -1 : 1;
            else
                result = string.CompareOrdinal(left[i], right[i]);
            if (result != 0)
                return result;
        }
        return 0;
    }
}
