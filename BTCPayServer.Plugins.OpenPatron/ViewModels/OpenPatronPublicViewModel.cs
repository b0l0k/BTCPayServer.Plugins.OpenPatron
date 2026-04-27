using System.Collections.Generic;

namespace BTCPayServer.Plugins.OpenPatron.ViewModels;

public class OpenPatronPublicViewModel
{
    public string AppId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string? OfferingId { get; set; }
    public string PublicPageUrl { get; set; } = string.Empty;
    public bool SupportsOneTime { get; set; }
    public bool SupportsSubscriptions { get; set; }
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroSubtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PrimaryCallToAction { get; set; } = string.Empty;
    public string? PrimaryCallToActionUrl { get; set; }
    public string DefaultCurrency { get; set; } = "USD";
    public IReadOnlyList<decimal> SuggestedAmounts { get; set; } = [];
    public IReadOnlyList<OpenPatronPublicLinkViewModel> Links { get; set; } = [];
    public IReadOnlyList<OpenPatronPublicPlanViewModel> Plans { get; set; } = [];
}

public class OpenPatronPublicLinkViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
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

public class OpenPatronOneTimeOptionViewModel
{
    public decimal Amount { get; set; }
    public string Label { get; set; } = string.Empty;
}
