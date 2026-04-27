using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.OpenPatron.Models;

public class OpenPatronAppSettings
{
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

public class OpenPatronLink
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
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
