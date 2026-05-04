using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Plugins.OpenPatron.Models;

public class OpenPatronAppSettings
{
    // Template metadata
    public OpenPatronPageType PageType { get; set; } = OpenPatronPageType.Project;
    public bool PageTypeConfirmed { get; set; }

    // Section-based page layout
    public string PageLayoutPreset { get; set; } = "8-4";
    public List<PageSection>? Sections { get; set; }
    public PageTheme? Theme { get; set; }

    // Legacy flat layout (migrated to Sections on first load)
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<BlockDefinition>? PageLayout { get; set; }

    // Global settings
    public string? OfferingId { get; set; }
    public OpenPatronSupportMode SupportMode { get; set; } = OpenPatronSupportMode.Both;
    public string DefaultCurrency { get; set; } = "USD";
    public OpenPatronVisibility Visibility { get; set; } = OpenPatronVisibility.Unpublished;

    // Legacy (kept for JSON compat on old data, ignored by new code)
    public string? AccentColor { get; set; }
    public string? PrimaryCallToActionUrl { get; set; }
    public List<OpenPatronLink> Links { get; set; } = [];
}

public class PageSection
{
    public string Id { get; set; } = string.Empty;
    public int Width { get; set; }
    public List<BlockDefinition> Blocks { get; set; } = [];
}

public class BlockDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Type { get; set; } = string.Empty;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public JObject? Settings { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public BlockTheme? Theme { get; set; }
}

public class BlockTheme
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? AccentColor { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? BorderRadius { get; set; }
}

public class PageTheme
{
    public const string DefaultAccentColor = "#6366f1";

    public string AccentColor { get; set; } = DefaultAccentColor;
    public string BorderRadius { get; set; } = "1.5rem";
    public string BlockSpacing { get; set; } = "1rem";
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
