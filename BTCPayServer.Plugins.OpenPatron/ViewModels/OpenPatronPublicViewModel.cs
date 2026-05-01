using System;
using System.Collections.Generic;
using BTCPayServer.Plugins.OpenPatron.Models;

namespace BTCPayServer.Plugins.OpenPatron.ViewModels;

public class OpenPatronPublicViewModel
{
    public string AppId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string? OfferingId { get; set; }
    public string PublicPageUrl { get; set; } = string.Empty;
    public bool SupportsOneTime { get; set; }
    public bool SupportsSubscriptions { get; set; }

    // Page type (informational)
    public OpenPatronPageType PageType { get; set; } = OpenPatronPageType.Project;

    // Block layout
    public List<BlockDefinition> PageLayout { get; set; } = [];
    public PageTheme Theme { get; set; } = new();

    // Profile
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? GravatarUrl { get; set; }
    public string? GitHubProfileUrl { get; set; }

    // Projects (Personal page)
    public IReadOnlyList<OpenPatronPublicProjectViewModel> Projects { get; set; } = [];

    // Social links
    public string? SocialX { get; set; }
    public string? SocialMastodon { get; set; }
    public string? SocialNostr { get; set; }

    // Funding goal
    public decimal? FundingGoal { get; set; }
    public decimal AmountRaised { get; set; }
    public int FundingPercentage { get; set; }

    // Sponsor wall
    public bool ShowSponsorWall { get; set; }
    public IReadOnlyList<SponsorWallEntryViewModel> SponsorWallEntries { get; set; } = [];

    // Core settings
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroSubtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PrimaryCallToAction { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "USD";
    public IReadOnlyList<decimal> SuggestedAmounts { get; set; } = [];
    public IReadOnlyList<OpenPatronPublicPlanViewModel> Plans { get; set; } = [];
}

public class OpenPatronPublicProjectViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Language { get; set; }
    public int? Stars { get; set; }
}

public class SponsorWallEntryViewModel
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}

public class OpenPatronPublicPlanViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public string SubscribeUrl { get; set; } = string.Empty;
    public bool HasTrial { get; set; }
    public string? TrialLabel { get; set; }
}
