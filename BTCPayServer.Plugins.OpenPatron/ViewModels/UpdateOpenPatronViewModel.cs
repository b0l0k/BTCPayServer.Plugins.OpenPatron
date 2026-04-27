using System.ComponentModel.DataAnnotations;
using BTCPayServer.Plugins.OpenPatron.Models;

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

    [Display(Name = "Funding mode")]
    public OpenPatronSupportMode SupportMode { get; set; } = OpenPatronSupportMode.Both;

    [Required]
    [Display(Name = "App name")]
    [MaxLength(50)]
    public string AppName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Hero title")]
    [MaxLength(80)]
    public string HeroTitle { get; set; } = string.Empty;

    [Display(Name = "Hero subtitle")]
    [MaxLength(140)]
    public string HeroSubtitle { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Primary CTA")]
    [MaxLength(40)]
    public string PrimaryCallToAction { get; set; } = string.Empty;

    [Display(Name = "Primary CTA URL")]
    [Url]
    public string? PrimaryCallToActionUrl { get; set; }

    [Required]
    [Display(Name = "Default currency")]
    [MaxLength(12)]
    public string DefaultCurrency { get; set; } = "USD";

    [Display(Name = "Suggested amounts")]
    public string SuggestedAmounts { get; set; } = string.Empty;

    [Display(Name = "Project URL")]
    [Url]
    public string? ProjectUrl { get; set; }

    [Display(Name = "GitHub URL")]
    [Url]
    public string? GitHubUrl { get; set; }

    [Display(Name = "Status")]
    public OpenPatronVisibility Visibility { get; set; } = OpenPatronVisibility.Unpublished;
}
