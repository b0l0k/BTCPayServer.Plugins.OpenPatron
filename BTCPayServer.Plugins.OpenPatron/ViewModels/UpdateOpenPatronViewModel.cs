using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BTCPayServer.Plugins.OpenPatron.Models;
using BTCPayServer.Plugins.OpenPatron.Services;

namespace BTCPayServer.Plugins.OpenPatron.ViewModels;

public class UpdateOpenPatronViewModel
{
    public string AppId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string PublicPageUrl { get; set; } = string.Empty;
    public string? OfferingId { get; set; }
    public string? ManageOfferingUrl { get; set; }
    public string? AddPlanUrl { get; set; }
    public int ActivePlanCount { get; set; }
    public bool Archived { get; set; }

    // Page type (used for initial template selection)
    [Display(Name = "Page type")]
    public OpenPatronPageType PageType { get; set; } = OpenPatronPageType.Project;
    public bool PageTypeConfirmed { get; set; }

    // Block layout (JSON-serialized list of BlockDefinition)
    public string PageLayoutJson { get; set; } = "[]";

    // Theme
    [Display(Name = "Accent color")]
    [RegularExpression(@"^#[0-9a-fA-F]{6}$", ErrorMessage = "Must be a hex color like #6366f1.")]
    public string? ThemeAccentColor { get; set; }

    [Display(Name = "Border radius")]
    public string ThemeBorderRadius { get; set; } = "1.5rem";

    [Display(Name = "Block spacing")]
    public string ThemeBlockSpacing { get; set; } = "1rem";

    // Profile
    [Display(Name = "Display name")]
    [MaxLength(80)]
    public string? DisplayName { get; set; }

    [Display(Name = "Bio")]
    [MaxLength(500)]
    public string? Bio { get; set; }

    [Display(Name = "GitHub username")]
    [MaxLength(39)]
    [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?$", ErrorMessage = "Invalid GitHub username.")]
    public string? GitHubUsername { get; set; }

    [Display(Name = "Gravatar email")]
    [EmailAddress]
    public string? GravatarEmail { get; set; }

    // Projects (Personal page)
    public List<UpdateOpenPatronProjectViewModel> Projects { get; set; } = [];
    public List<GitHubRepo> AvailableGitHubRepos { get; set; } = [];

    // Social links
    [Display(Name = "X (Twitter)")]
    [MaxLength(120)]
    public string? SocialX { get; set; }

    [Display(Name = "Mastodon")]
    [MaxLength(200)]
    [Url]
    public string? SocialMastodon { get; set; }

    [Display(Name = "Nostr (npub)")]
    [MaxLength(200)]
    public string? SocialNostr { get; set; }

    // Appearance (legacy field, kept for compat)
    [Display(Name = "Accent color")]
    [RegularExpression(@"^#[0-9a-fA-F]{6}$", ErrorMessage = "Must be a hex color like #6366f1.")]
    public string? AccentColor { get; set; }

    // Funding goal
    [Display(Name = "Funding goal")]
    [Range(0.01, 999999999, ErrorMessage = "Must be greater than 0.")]
    public decimal? FundingGoal { get; set; }

    // Sponsor wall
    [Display(Name = "Show sponsor wall")]
    public bool ShowSponsorWall { get; set; }

    // Core settings
    [Display(Name = "Funding mode")]
    public OpenPatronSupportMode SupportMode { get; set; } = OpenPatronSupportMode.Both;

    [Required]
    [Display(Name = "Internal name")]
    [MaxLength(50)]
    public string AppName { get; set; } = string.Empty;

    [Display(Name = "Public headline")]
    [MaxLength(80)]
    public string HeroTitle { get; set; } = string.Empty;

    [Display(Name = "Tagline")]
    [MaxLength(140)]
    public string HeroSubtitle { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Primary CTA")]
    [MaxLength(40)]
    public string PrimaryCallToAction { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Default currency")]
    [MaxLength(12)]
    public string DefaultCurrency { get; set; } = "USD";

    [Display(Name = "Suggested amounts")]
    public string SuggestedAmounts { get; set; } = string.Empty;

    [Display(Name = "Status")]
    public OpenPatronVisibility Visibility { get; set; } = OpenPatronVisibility.Unpublished;
}

public class UpdateOpenPatronProjectViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Language { get; set; }
    public int? Stars { get; set; }
}
