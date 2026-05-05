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
    public const string OneTimePayment = "one-time-payment";

    private static readonly Dictionary<string, BlockTypeInfo> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [ProfileHero] = new("Profile Hero", "Avatar, name, bio, and social links", "/Views/UIOpenPatron/Blocks/_Block_ProfileHero.cshtml", typeof(ProfileHeroSettings)),
        [ProjectHero] = new("Project Hero", "Title, subtitle, and support badges", "/Views/UIOpenPatron/Blocks/_Block_ProjectHero.cshtml", typeof(ProjectHeroSettings)),
        [FundingProgress] = new("Funding Progress", "Progress bar toward funding goal", "/Views/UIOpenPatron/Blocks/_Block_FundingProgress.cshtml", typeof(FundingProgressSettings)),
        [Description] = new("Description", "About section with text content", "/Views/UIOpenPatron/Blocks/_Block_Description.cshtml", typeof(DescriptionSettings)),
        [ProjectsGrid] = new("Projects Grid", "Grid of open source project cards", "/Views/UIOpenPatron/Blocks/_Block_ProjectsGrid.cshtml", typeof(ProjectsGridSettings)),
        [SubscriptionTiers] = new("Subscription Tiers", "Sponsor plan cards with subscribe actions", "/Views/UIOpenPatron/Blocks/_Block_SubscriptionTiers.cshtml", typeof(SubscriptionTiersSettings)),
        [QuickSupport] = new("Quick Support", "Suggested one-time amount buttons", "/Views/UIOpenPatron/Blocks/_Block_QuickSupport.cshtml", typeof(QuickSupportSettings)),
        [SponsorWall] = new("Sponsor Wall", "Recent anonymous contributions", "/Views/UIOpenPatron/Blocks/_Block_SponsorWall.cshtml", typeof(SponsorWallSettings)),
        [OneTimePayment] = new("One-Time Payment", "Compact sponsor panel with contribution form", "/Views/UIOpenPatron/Blocks/_Block_OneTimePayment.cshtml", typeof(OneTimePaymentSettings)),
    };

    public static IReadOnlyDictionary<string, BlockTypeInfo> AllTypes { get; } = new ReadOnlyDictionary<string, BlockTypeInfo>(Types);

    public static string? GetPartialViewPath(string blockType)
        => Types.TryGetValue(blockType, out var info) ? info.PartialViewPath : null;

    public static bool IsKnownType(string blockType)
        => Types.ContainsKey(blockType);

    public static Type? GetSettingsType(string blockType)
        => Types.TryGetValue(blockType, out var info) ? info.SettingsType : null;

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
                new() { Type = ProfileHero, Settings = JObject.FromObject(new ProfileHeroSettings()) },
                new() { Type = Description, Settings = JObject.FromObject(new DescriptionSettings { Heading = "What I work on" }) },
                new() { Type = ProjectsGrid, Settings = JObject.FromObject(new ProjectsGridSettings()) },
                new() { Type = SubscriptionTiers, Settings = JObject.FromObject(new SubscriptionTiersSettings { Heading = "Choose a sponsorship tier", Subtitle = "Pick the level that fits you best" }) },
                new() { Type = QuickSupport, Settings = JObject.FromObject(new QuickSupportSettings { Heading = "Send quick support" }) },
                new() { Type = SponsorWall, Settings = JObject.FromObject(new SponsorWallSettings { Heading = "Supporters" }) },
            ]
        },
        new()
        {
            Id = "col-2", Width = 4, Blocks =
            [
                new() { Type = OneTimePayment, Settings = JObject.FromObject(new OneTimePaymentSettings { Heading = "Sponsor now" }) },
            ]
        }
    ];

    public static List<PageSection> DefaultSectionsForProject() =>
    [
        new()
        {
            Id = "col-1", Width = 8, Blocks =
            [
                new() { Type = ProjectHero, Settings = JObject.FromObject(new ProjectHeroSettings()) },
                new() { Type = FundingProgress, Settings = JObject.FromObject(new FundingProgressSettings()) },
                new() { Type = Description, Settings = JObject.FromObject(new DescriptionSettings { Heading = "Why sponsor this work?" }) },
                new() { Type = SubscriptionTiers, Settings = JObject.FromObject(new SubscriptionTiersSettings { Heading = "Choose a sponsorship tier", Subtitle = "Pick the level that fits you best" }) },
                new() { Type = QuickSupport, Settings = JObject.FromObject(new QuickSupportSettings { Heading = "Send quick support" }) },
                new() { Type = SponsorWall, Settings = JObject.FromObject(new SponsorWallSettings { Heading = "Who's supporting this work" }) },
            ]
        },
        new()
        {
            Id = "col-2", Width = 4, Blocks =
            [
                new() { Type = OneTimePayment, Settings = JObject.FromObject(new OneTimePaymentSettings { Heading = "Sponsor now" }) },
            ]
        }
    ];

    public static List<PageSection>? GetTemplateSections(string? template) => template switch
    {
        "personal" => DefaultSectionsForPersonal(),
        "project" => DefaultSectionsForProject(),
        "empty" => null,
        _ => null
    };

    public static IEnumerable<BlockDefinition> AllBlocks(List<PageSection>? sections) =>
        sections?.SelectMany(s => s.Blocks) ?? [];
}

public record BlockTypeInfo(string DisplayName, string Description, string PartialViewPath, Type SettingsType);
