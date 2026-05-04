using System;
using System.Collections.Generic;
using BTCPayServer.Plugins.OpenPatron.Models;
using BTCPayServer.Plugins.OpenPatron.Services;

namespace BTCPayServer.Plugins.OpenPatron.ViewModels;

public class OpenPatronPublicViewModel
{
    public string AppId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string PublicPageUrl { get; set; } = string.Empty;
    public bool SupportsOneTime { get; set; }
    public bool SupportsSubscriptions { get; set; }
    public string DefaultCurrency { get; set; } = "USD";

    // Section-based layout
    public List<PageSection> Sections { get; set; } = [];
    public PageTheme Theme { get; set; } = new();

    // Legacy (flat list, for compat in block partials that reference Model.Page.PageLayout)
    public List<BlockDefinition> PageLayout { get; set; } = [];

    // Runtime/computed data (populated by controller, consumed by block partials)
    public decimal AmountRaised { get; set; }
    public int FundingPercentage { get; set; }
    public IReadOnlyList<SponsorWallEntry> SponsorWallEntries { get; set; } = [];
    public IReadOnlyList<OpenPatronPublicPlanViewModel> Plans { get; set; } = [];
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
