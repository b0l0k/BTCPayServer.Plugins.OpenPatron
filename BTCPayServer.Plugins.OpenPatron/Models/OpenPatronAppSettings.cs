using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.OpenPatron.Models;

public class OpenPatronAppSettings
{
    // Page type
    public OpenPatronPageType PageType { get; set; } = OpenPatronPageType.Project;
    public bool PageTypeConfirmed { get; set; }

    // Profile
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? GitHubUsername { get; set; }
    public string? GravatarEmail { get; set; }

    // Projects (Personal pages)
    public List<OpenPatronProject> Projects { get; set; } = [];

    // Social links
    public OpenPatronSocialLinks? SocialLinks { get; set; }

    // Appearance
    public string? AccentColor { get; set; }

    // Funding goal
    public decimal? FundingGoal { get; set; }

    // Sponsor wall
    public bool ShowSponsorWall { get; set; }

    // Core settings
    public string? OfferingId { get; set; }
    public OpenPatronSupportMode SupportMode { get; set; } = OpenPatronSupportMode.Both;
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroSubtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PrimaryCallToAction { get; set; } = string.Empty;
    public string? PrimaryCallToActionUrl { get; set; }
    public string DefaultCurrency { get; set; } = "USD";
    public List<decimal> SuggestedAmounts { get; set; } = [];
    public List<OpenPatronLink> Links { get; set; } = [];
    public OpenPatronVisibility Visibility { get; set; } = OpenPatronVisibility.Unpublished;
}

public class OpenPatronProject
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Language { get; set; }
    public int? Stars { get; set; }
}

public class OpenPatronSocialLinks
{
    public string? X { get; set; }
    public string? Mastodon { get; set; }
    public string? Nostr { get; set; }
}

public class OpenPatronLink
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public enum OpenPatronPageType
{
    [Display(Name = "Personal")]
    Personal = 0,
    [Display(Name = "Project")]
    Project = 1
}

public enum OpenPatronVisibility
{
    [Display(Name = "Not published")]
    Unpublished = 0,
    [Display(Name = "Published")]
    Published = 1
}

public enum OpenPatronSupportMode
{
    OneTimeOnly = 0,
    SubscriptionOnly = 1,
    Both = 2
}
