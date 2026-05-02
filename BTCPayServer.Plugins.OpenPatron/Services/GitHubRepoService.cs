using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace BTCPayServer.Plugins.OpenPatron.Services;

public class GitHubRepoService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Regex GitHubRepoUrlPattern = new(
        @"^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/?#]+)/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public static bool TryParseGitHubUrl(string? url, out string owner, out string repo)
    {
        owner = repo = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var match = GitHubRepoUrlPattern.Match(url.Trim());
        if (!match.Success)
            return false;

        owner = match.Groups["owner"].Value;
        repo = match.Groups["repo"].Value;

        if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repo = repo[..^4];

        return !string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo);
    }

    public async Task<GitHubRepo?> GetRepoAsync(string owner, string repo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            return null;

        var cacheKey = $"gh_repo:{owner.ToLowerInvariant()}/{repo.ToLowerInvariant()}";

        if (memoryCache.TryGetValue(cacheKey, out GitHubRepo? cached))
            return cached;

        try
        {
            using var client = CreateClient();
            var url = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}";
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<GitHubRepo>(json, JsonOptions);

            if (result is not null)
                memoryCache.Set(cacheKey, result, CacheDuration);

            return result;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<GitHubRepo>> GetPublicReposAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return [];

        try
        {
            using var client = CreateClient();
            var url = $"https://api.github.com/users/{Uri.EscapeDataString(username.Trim())}/repos?type=owner&sort=updated&per_page=30";
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return [];

            var json = await response.Content.ReadAsStringAsync(ct);
            var repos = JsonSerializer.Deserialize<List<GitHubRepo>>(json, JsonOptions);
            return repos?
                .Where(r => !r.Fork && !r.Archived)
                .OrderByDescending(r => r.StargazersCount)
                .ThenByDescending(r => r.UpdatedAt)
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("GitHub");
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BTCPayServer-OpenPatron", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}

public class GitHubRepo
{
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Language { get; set; }

    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; set; }

    public bool Fork { get; set; }
    public bool Archived { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
