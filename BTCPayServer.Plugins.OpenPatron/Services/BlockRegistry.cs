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
                new() { Type = ProfileHero, Settings = JObject.FromObject(new ProfileHeroSettings
                {
                    DisplayName = "Nicolas Dorier",
                    Subtitle = "Bitcoin Developer & Open-Source Builder",
                    Bio = "Building freedom tech. Creator of BTCPay Server, an open-source Bitcoin payment processor. Author of \"Programming The Blockchain in C#\".",
                    SocialX = "NicolasDorier",
                    SocialNostr = "npub1yncz5kqzjxqxlhm0xpv40t2g5lksmvy7sh09apxkx6725mytctqcmudak",
                    GitHubUsername = "NicolasDorier",
                }) },
                new() { Type = Description, Settings = JObject.FromObject(new DescriptionSettings
                {
                    Heading = "What I work on",
                    Content = "I spend my time building open-source tools that empower individuals and merchants to accept Bitcoin without intermediaries. BTCPay Server is used by thousands of merchants worldwide and is entirely community-funded.\n\nYour support helps me continue working full-time on freedom tech — no corporate sponsors, no strings attached.",
                }) },
                new() { Type = ProjectsGrid, Settings = JObject.FromObject(new ProjectsGridSettings
                {
                    ColumnsPerRow = 2,
                    Projects =
                    [
                        new() { Name = "BTCPay Server", Url = "https://github.com/btcpayserver/btcpayserver", Description = "Open-source Bitcoin payment processor" },
                        new() { Name = "NBitcoin", Url = "https://github.com/MetacoSA/NBitcoin", Description = "Comprehensive Bitcoin library for .NET" },
                        new() { Name = "NBXplorer", Url = "https://github.com/dgarage/NBXplorer", Description = "Minimalist UTXO tracker for HD wallets" },
                        new() { Name = "Programming The Blockchain in C#", Url = "https://programmingblockchain.gitbook.io", Description = "Free book to learn Bitcoin development" },
                    ],
                }) },
                new() { Type = SubscriptionTiers, Settings = JObject.FromObject(new SubscriptionTiersSettings { Heading = "Become a sponsor", Subtitle = "Pick the level that fits you best" }) },
                new() { Type = QuickSupport, Settings = JObject.FromObject(new QuickSupportSettings { Heading = "Send a tip", SuggestedAmounts = [5, 21, 50, 100] }) },
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
                new() { Type = ProjectHero, Settings = JObject.FromObject(new ProjectHeroSettings
                {
                    Title = "BTCPay Server v3 — The Next Chapter",
                    Subtitle = "Help us ship the most ambitious BTCPay Server release yet",
                    DisplayName = "BTCPay Server Foundation",
                    GitHubUsername = "btcpayserver",
                    SocialX = "BtcpayServer",
                    SocialNostr = "npub1r22dt8xraqj42x2kk49ycmpnfmfzc3faxap2kd3f0gpe4ul68nhsaxlas2",
                }) },
                new() { Type = FundingProgress, Settings = JObject.FromObject(new FundingProgressSettings { Goal = 21000, ProgressBarStyle = "gradient" }) },
                new() { Type = Description, Settings = JObject.FromObject(new DescriptionSettings
                {
                    Heading = "Why sponsor this work?",
                    Content = "BTCPay Server v3 is a ground-up rebuild of the plugin architecture, bringing a new dashboard, faster sync times, and first-class multi-tenant support.\n\nThis release requires months of dedicated full-time work from core contributors. Every contribution — large or small — goes directly toward developer salaries, infrastructure, and security audits.\n\nNo VCs. No token sales. Just open-source software funded by the people who use it.",
                }) },
                new() { Type = SubscriptionTiers, Settings = JObject.FromObject(new SubscriptionTiersSettings { Heading = "Become a project sponsor", Subtitle = "Recurring support keeps development sustainable" }) },
                new() { Type = QuickSupport, Settings = JObject.FromObject(new QuickSupportSettings { Heading = "One-time contribution", SuggestedAmounts = [21, 100, 500, 1000] }) },
                new() { Type = SponsorWall, Settings = JObject.FromObject(new SponsorWallSettings { Heading = "Who's supporting this work" }) },
            ]
        },
        new()
        {
            Id = "col-2", Width = 4, Blocks =
            [
                new() { Type = OneTimePayment, Settings = JObject.FromObject(new OneTimePaymentSettings { Heading = "Fund the next release" }) },
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
