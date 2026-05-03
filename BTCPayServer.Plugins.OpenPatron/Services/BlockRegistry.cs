using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BTCPayServer.Plugins.OpenPatron.Models;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Plugins.OpenPatron.Services;

public static class BlockRegistry
{
    public const string ProfileHero = "profile-hero";
    public const string ProjectHero = "project-hero";
    public const string FundingProgress = "funding-progress";
    public const string Description = "description";
    public const string ProjectsGrid = "projects-grid";
    public const string SubscriptionTiers = "subscription-tiers";
    public const string QuickSupport = "quick-support";
    public const string SponsorWall = "sponsor-wall";
    public const string SidebarSupport = "sidebar-support";

    private static readonly Dictionary<string, BlockTypeInfo> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [ProfileHero] = new("Profile Hero", "Avatar, name, bio, and social links", "/Views/UIOpenPatron/Blocks/_Block_ProfileHero.cshtml"),
        [ProjectHero] = new("Project Hero", "Title, subtitle, and support badges", "/Views/UIOpenPatron/Blocks/_Block_ProjectHero.cshtml"),
        [FundingProgress] = new("Funding Progress", "Progress bar toward funding goal", "/Views/UIOpenPatron/Blocks/_Block_FundingProgress.cshtml"),
        [Description] = new("Description", "About section with text content", "/Views/UIOpenPatron/Blocks/_Block_Description.cshtml"),
        [ProjectsGrid] = new("Projects Grid", "Grid of open source project cards", "/Views/UIOpenPatron/Blocks/_Block_ProjectsGrid.cshtml"),
        [SubscriptionTiers] = new("Subscription Tiers", "Sponsor plan cards with subscribe actions", "/Views/UIOpenPatron/Blocks/_Block_SubscriptionTiers.cshtml"),
        [QuickSupport] = new("Quick Support", "Suggested one-time amount buttons", "/Views/UIOpenPatron/Blocks/_Block_QuickSupport.cshtml"),
        [SponsorWall] = new("Sponsor Wall", "Recent anonymous contributions", "/Views/UIOpenPatron/Blocks/_Block_SponsorWall.cshtml"),
        [SidebarSupport] = new("Sidebar Support", "Compact sponsor panel with contribution form", "/Views/UIOpenPatron/Blocks/_Block_SidebarSupport.cshtml"),
    };

    public static IReadOnlyDictionary<string, BlockTypeInfo> AllTypes { get; } = new ReadOnlyDictionary<string, BlockTypeInfo>(Types);

    public static string? GetPartialViewPath(string blockType)
        => Types.TryGetValue(blockType, out var info) ? info.PartialViewPath : null;

    public static bool IsKnownType(string blockType)
        => Types.ContainsKey(blockType);

    // ── Layout presets ──

    public static readonly IReadOnlyDictionary<string, int[]> LayoutPresets = new ReadOnlyDictionary<string, int[]>(
        new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["8-4"] = [8, 4],
            ["4-8"] = [4, 8],
            ["6-6"] = [6, 6],
            ["12"] = [12],
        });

    public static List<PageSection> CreateSectionsForPreset(string preset)
    {
        if (!LayoutPresets.TryGetValue(preset, out var widths))
            widths = [8, 4];

        return widths.Select((w, i) => new PageSection
        {
            Id = $"col-{i + 1}",
            Width = w,
            Blocks = []
        }).ToList();
    }

    // ── Default layouts (now section-based) ──

    public static List<PageSection> DefaultSectionsForPersonal() =>
    [
        new()
        {
            Id = "col-1", Width = 8, Blocks =
            [
                new() { Type = ProfileHero, Settings = JObject.FromObject(new { displayName = "", bio = "", subtitle = "", gravatarEmail = "", gitHubUsername = "", socialX = "", socialMastodon = "", socialNostr = "" }) },
                new() { Type = FundingProgress, Settings = JObject.FromObject(new { goal = 0 }) },
                new() { Type = Description, Settings = JObject.FromObject(new { heading = "What I work on", content = "" }) },
                new() { Type = ProjectsGrid, Settings = JObject.FromObject(new { projects = new object[0] }) },
                new() { Type = SubscriptionTiers, Settings = JObject.FromObject(new { heading = "Choose a sponsorship tier", subtitle = "Pick the level that fits you best" }) },
                new() { Type = QuickSupport, Settings = JObject.FromObject(new { heading = "Send quick support", suggestedAmounts = new object[0] }) },
                new() { Type = SponsorWall, Settings = JObject.FromObject(new { heading = "Supporters" }) },
            ]
        },
        new()
        {
            Id = "col-2", Width = 4, Blocks =
            [
                new() { Type = SidebarSupport, Settings = JObject.FromObject(new { heading = "Sponsor now" }) },
            ]
        }
    ];

    public static List<PageSection> DefaultSectionsForProject() =>
    [
        new()
        {
            Id = "col-1", Width = 8, Blocks =
            [
                new() { Type = ProjectHero, Settings = JObject.FromObject(new { title = "", subtitle = "", displayName = "", gravatarEmail = "", gitHubUsername = "", socialX = "", socialMastodon = "", socialNostr = "" }) },
                new() { Type = FundingProgress, Settings = JObject.FromObject(new { goal = 0 }) },
                new() { Type = Description, Settings = JObject.FromObject(new { heading = "Why sponsor this work?", content = "" }) },
                new() { Type = SubscriptionTiers, Settings = JObject.FromObject(new { heading = "Choose a sponsorship tier", subtitle = "Pick the level that fits you best" }) },
                new() { Type = QuickSupport, Settings = JObject.FromObject(new { heading = "Send quick support", suggestedAmounts = new object[0] }) },
                new() { Type = SponsorWall, Settings = JObject.FromObject(new { heading = "Who's supporting this work" }) },
            ]
        },
        new()
        {
            Id = "col-2", Width = 4, Blocks =
            [
                new() { Type = SidebarSupport, Settings = JObject.FromObject(new { heading = "Sponsor now" }) },
            ]
        }
    ];

    public static List<PageSection> DefaultSectionsFor(OpenPatronPageType pageType) =>
        pageType == OpenPatronPageType.Personal
            ? DefaultSectionsForPersonal()
            : DefaultSectionsForProject();

    // Legacy compat (flat block list from sections)
    public static List<BlockDefinition> DefaultLayoutFor(OpenPatronPageType pageType) =>
        DefaultSectionsFor(pageType).SelectMany(s => s.Blocks).ToList();

    public static List<BlockDefinition> DefaultLayoutForPersonal() =>
        DefaultSectionsForPersonal().SelectMany(s => s.Blocks).ToList();

    public static List<BlockDefinition> DefaultLayoutForProject() =>
        DefaultSectionsForProject().SelectMany(s => s.Blocks).ToList();

    public static IEnumerable<BlockDefinition> AllBlocks(List<PageSection>? sections) =>
        sections?.SelectMany(s => s.Blocks) ?? [];
}

public record BlockTypeInfo(string DisplayName, string Description, string PartialViewPath);
