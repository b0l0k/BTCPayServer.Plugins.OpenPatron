using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Plugins.OpenPatron.Models;

public class OpenPatronAppSettings
{
    // Section-based page layout
    public string PageLayoutPreset { get; set; } = "8-4";
    public List<PageSection>? Sections { get; set; }

    // True once the user has picked a template (including "empty") or saved settings.
    // Distinguishes a freshly-created app from one explicitly initialized with no blocks.
    public bool Initialized { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public PageTheme? Theme { get; set; }

    // Legacy flat layout (migrated to Sections on first load)
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<BlockDefinition>? PageLayout { get; set; }

    // Global settings
    public string? OfferingId { get; set; }
    public string DefaultCurrency { get; set; } = "USD";
    public OpenPatronVisibility Visibility { get; set; } = OpenPatronVisibility.Unpublished;
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
    public JObject Settings { get; set; } = new();

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
    public const string DefaultAccentColor = "#51b13e";
    public const string DefaultSecondaryColor = "#CEDC21";
    public const string DefaultBorderRadius = "8px";
    public const string DefaultBlockSpacing = "1rem";
    public const string DefaultShadowStyle = "subtle";
    public const string DefaultTypographyStyle = "standard";
    public const string DefaultBackgroundStyle = "flat";

    public string AccentColor { get; set; } = DefaultAccentColor;
    public string SecondaryColor { get; set; } = DefaultSecondaryColor;
    public string BorderRadius { get; set; } = DefaultBorderRadius;
    public string BlockSpacing { get; set; } = DefaultBlockSpacing;
    public string ShadowStyle { get; set; } = DefaultShadowStyle;
    public string TypographyStyle { get; set; } = DefaultTypographyStyle;
    public string BackgroundStyle { get; set; } = DefaultBackgroundStyle;

    public bool IsDefault() =>
        AccentColor == DefaultAccentColor &&
        SecondaryColor == DefaultSecondaryColor &&
        BorderRadius == DefaultBorderRadius &&
        BlockSpacing == DefaultBlockSpacing &&
        ShadowStyle == DefaultShadowStyle &&
        TypographyStyle == DefaultTypographyStyle &&
        BackgroundStyle == DefaultBackgroundStyle;
}

public enum OpenPatronVisibility
{
    [Display(Name = "Not published")]
    Unpublished = 0,
    [Display(Name = "Published")]
    Published = 1
}

