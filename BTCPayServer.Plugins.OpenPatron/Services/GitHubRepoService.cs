using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.OpenPatron.Services;

public class GitHubRepoService(IHttpClientFactory httpClientFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<List<GitHubRepo>> GetPublicReposAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return [];

        try
        {
            using var client = httpClientFactory.CreateClient("GitHub");
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BTCPayServer-OpenPatron", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

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
