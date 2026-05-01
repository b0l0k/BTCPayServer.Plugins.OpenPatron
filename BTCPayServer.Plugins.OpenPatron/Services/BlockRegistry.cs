using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTCPayServer.Plugins.OpenPatron.Models;

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
    };

    public static IReadOnlyDictionary<string, BlockTypeInfo> AllTypes { get; } = new ReadOnlyDictionary<string, BlockTypeInfo>(Types);

    public static string? GetPartialViewPath(string blockType)
        => Types.TryGetValue(blockType, out var info) ? info.PartialViewPath : null;

    public static bool IsKnownType(string blockType)
        => Types.ContainsKey(blockType);

    public static List<BlockDefinition> DefaultLayoutForPersonal() =>
    [
        new() { Type = ProfileHero },
        new() { Type = FundingProgress },
        new() { Type = Description },
        new() { Type = ProjectsGrid },
        new() { Type = SubscriptionTiers },
        new() { Type = QuickSupport },
        new() { Type = SponsorWall },
    ];

    public static List<BlockDefinition> DefaultLayoutForProject() =>
    [
        new() { Type = ProjectHero },
        new() { Type = FundingProgress },
        new() { Type = Description },
        new() { Type = SubscriptionTiers },
        new() { Type = QuickSupport },
        new() { Type = SponsorWall },
    ];

    public static List<BlockDefinition> DefaultLayoutFor(OpenPatronPageType pageType) =>
        pageType == OpenPatronPageType.Personal
            ? DefaultLayoutForPersonal()
            : DefaultLayoutForProject();
}

public record BlockTypeInfo(string DisplayName, string Description, string PartialViewPath);
