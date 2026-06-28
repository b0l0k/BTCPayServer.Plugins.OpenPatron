using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BTCPayServer.Plugins.OpenPatron.Models;
using Markdig;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Plugins.OpenPatron.Services;

public static class BlockSettingsHelper
{
    public static T? GetTyped<T>(BlockDefinition block) where T : class
    {
        if (block.Settings is null)
            return null;

        var settings = (JObject)block.Settings.DeepClone();
        RemoveTokensThatShouldUseDefaults<T>(settings);
        return settings.ToObject<T>();
    }

    private static void RemoveTokensThatShouldUseDefaults<T>(JObject settings)
    {
        foreach (var property in typeof(T).GetProperties())
        {
            var token = settings[property.Name];
            if (token is null)
                continue;

            if (token.Type is JTokenType.Null or JTokenType.Undefined)
            {
                settings.Remove(property.Name);
                continue;
            }

            var propertyType = property.PropertyType;
            var isNonNullableValueType = propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) is null;
            if (isNonNullableValueType &&
                token.Type == JTokenType.String &&
                string.IsNullOrWhiteSpace(token.Value<string>()))
            {
                settings.Remove(property.Name);
            }
        }
    }

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseAutoLinks()
        .Build();

    public static string RenderMarkdownToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;
        return Markdown.ToHtml(markdown, MarkdownPipeline);
    }

    public static string? Str(JObject? s, string key)
        => s?[key]?.Type == JTokenType.String ? s[key]!.Value<string>() : null;

    public static string Str(JObject? s, string key, string fallback)
        => Str(s, key) ?? fallback;

    public static decimal? Dec(JObject? s, string key)
        => s?[key] is JToken t && (t.Type is JTokenType.Float or JTokenType.Integer)
            ? t.Value<decimal>()
            : null;

    public static List<decimal> DecArray(JObject? s, string key)
    {
        if (s?[key] is not JArray arr) return [];
        return arr
            .Where(t => t.Type is JTokenType.Float or JTokenType.Integer)
            .Select(t => t.Value<decimal>())
            .ToList();
    }

    public static JArray? Arr(JObject? s, string key)
        => s?[key] as JArray;

    public static string? ComputeGravatarUrl(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var hash = ComputeMd5Hash(email.Trim().ToLowerInvariant());
        return $"https://www.gravatar.com/avatar/{hash}?s=200&d=identicon";
    }

    public static string? GravatarUrl(JObject? s)
        => ComputeGravatarUrl(Str(s, nameof(HeroSettingsBase.GravatarEmail)));

    public static string? AvatarUrl(JObject? s)
        => GravatarUrl(s) ?? GitHubAvatarUrl(s);

    public static string? GitHubAvatarUrl(JObject? s)
    {
        var username = Str(s, nameof(HeroSettingsBase.GitHubUsername));
        return string.IsNullOrWhiteSpace(username)
            ? null
            : $"https://github.com/{Uri.EscapeDataString(username.Trim())}.png?size=200";
    }

    public static string? GitHubProfileUrl(JObject? s)
    {
        var username = Str(s, nameof(HeroSettingsBase.GitHubUsername));
        return string.IsNullOrWhiteSpace(username)
            ? null
            : $"https://github.com/{Uri.EscapeDataString(username)}";
    }

    public static bool HasSocialLinks(JObject? s)
        => !string.IsNullOrWhiteSpace(Str(s, nameof(HeroSettingsBase.SocialX)))
        || !string.IsNullOrWhiteSpace(Str(s, nameof(HeroSettingsBase.SocialMastodon)))
        || !string.IsNullOrWhiteSpace(Str(s, nameof(HeroSettingsBase.SocialNostr)));

    public static string? BlockThemeStyle(BlockTheme? theme)
    {
        if (theme is null) return null;

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(theme.AccentColor))
            sb.Append($"--op-accent:{theme.AccentColor};--op-accent-10:{theme.AccentColor}1a;--op-accent-18:{theme.AccentColor}2e;--op-accent-dark:color-mix(in srgb,{theme.AccentColor} 85%,#000);");
        if (!string.IsNullOrWhiteSpace(theme.BorderRadius))
            sb.Append($"--op-border-radius:{theme.BorderRadius};");

        return sb.Length > 0 ? sb.ToString() : null;
    }

    public static string ComputeMd5Hash(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(32);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
