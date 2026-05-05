using System.ComponentModel.DataAnnotations;
using BTCPayServer.Plugins.OpenPatron.Models;

namespace BTCPayServer.Plugins.OpenPatron.ViewModels;

public class UpdateOpenPatronViewModel
{
    public string AppId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string PublicPageUrl { get; set; } = string.Empty;
    public string BadgeUrl { get; set; } = string.Empty;
    public string? OfferingId { get; set; }
    public string? ManageOfferingUrl { get; set; }
    public string? AddPlanUrl { get; set; }
    public int ActivePlanCount { get; set; }
    public bool Archived { get; set; }

    // Section-based layout (JSON-serialized)
    public string PageLayoutPreset { get; set; } = "8-4";
    public string SectionsJson { get; set; } = "[]";
    public bool HasBlocks { get; set; }

    // Theme
    [Display(Name = "Accent color")]
    [RegularExpression(@"^#[0-9a-fA-F]{6}$", ErrorMessage = "Must be a hex color like #6366f1.")]
    public string? ThemeAccentColor { get; set; }

    [Display(Name = "Secondary color")]
    [RegularExpression(@"^#[0-9a-fA-F]{6}$", ErrorMessage = "Must be a hex color like #ec4899.")]
    public string? ThemeSecondaryColor { get; set; }

    [Display(Name = "Border radius")]
    public string ThemeBorderRadius { get; set; } = PageTheme.DefaultBorderRadius;

    [Display(Name = "Block spacing")]
    public string ThemeBlockSpacing { get; set; } = PageTheme.DefaultBlockSpacing;

    [Display(Name = "Shadow style")]
    public string ThemeShadowStyle { get; set; } = PageTheme.DefaultShadowStyle;

    [Display(Name = "Typography")]
    public string ThemeTypographyStyle { get; set; } = PageTheme.DefaultTypographyStyle;

    [Display(Name = "Background")]
    public string ThemeBackgroundStyle { get; set; } = PageTheme.DefaultBackgroundStyle;

    // Global settings
    [Required]
    [Display(Name = "Internal name")]
    [MaxLength(50)]
    public string AppName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Default currency")]
    [MaxLength(12)]
    public string DefaultCurrency { get; set; } = "USD";

    [Display(Name = "Status")]
    public OpenPatronVisibility Visibility { get; set; } = OpenPatronVisibility.Unpublished;
}
