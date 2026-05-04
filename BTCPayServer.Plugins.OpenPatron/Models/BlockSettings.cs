using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.OpenPatron.Models;

public abstract class HeroSettingsBase
{
    [Description("Display name shown on the page")]
    public string DisplayName { get; set; } = "";

    [Description("Short subtitle below the name")]
    public string Subtitle { get; set; } = "";

    [Description("Email for Gravatar avatar")]
    public string GravatarEmail { get; set; } = "";

    [Description("GitHub username (also used as avatar fallback)")]
    public string GitHubUsername { get; set; } = "";

    [Description("X (Twitter) handle")]
    public string SocialX { get; set; } = "";

    [Description("Mastodon profile URL")]
    public string SocialMastodon { get; set; } = "";

    [Description("Nostr npub identifier")]
    public string SocialNostr { get; set; } = "";
}

[Description("Personal profile hero: avatar, name, bio, and social links")]
public class ProfileHeroSettings : HeroSettingsBase
{
    [Description("Brief biography / description")]
    public string Bio { get; set; } = "";
}

[Description("Project hero: title, subtitle, maintainer card, and social links")]
public class ProjectHeroSettings : HeroSettingsBase
{
    [Description("Main headline")]
    public string Title { get; set; } = "";
}

[Description("Progress bar toward a funding goal")]
public class FundingProgressSettings
{
    [Description("Funding goal amount (in default currency)")]
    [Range(0, double.MaxValue)]
    public decimal Goal { get; set; }
}

[Description("Rich text section with heading and markdown content")]
public class DescriptionSettings
{
    [Description("Section heading")]
    public string Heading { get; set; } = "";

    [Description("Markdown-formatted body content")]
    public string Content { get; set; } = "";
}

[Description("A project entry in the projects grid")]
public class ProjectItem
{
    [Description("Project name (auto-filled from GitHub if URL is a GitHub repo)")]
    public string Name { get; set; } = "";

    [Description("Project URL. GitHub repo URLs get dynamic stars and language at render time.")]
    public string Url { get; set; } = "";

    [Description("Short project description (auto-filled from GitHub if URL is a GitHub repo)")]
    public string Description { get; set; } = "";
}

[Description("Grid of open source project cards")]
public class ProjectsGridSettings
{
    [Description("List of projects to display")]
    public List<ProjectItem> Projects { get; set; } = [];

    [Description("Number of project cards per row (1-4)")]
    [Range(1, 4)]
    public int ColumnsPerRow { get; set; } = 2;
}

[Description("Subscription plan cards with subscribe actions")]
public class SubscriptionTiersSettings
{
    [Description("Section heading")]
    public string Heading { get; set; } = "";

    [Description("Subtitle below the heading")]
    public string Subtitle { get; set; } = "";

    [Description("Number of plan cards per row (1-4)")]
    [Range(1, 4)]
    public int ColumnsPerRow { get; set; } = 2;
}

[Description("Suggested one-time contribution amount buttons")]
public class QuickSupportSettings
{
    [Description("Section heading")]
    public string Heading { get; set; } = "";

    [Description("List of suggested amounts")]
    public List<decimal> SuggestedAmounts { get; set; } = [];
}

[Description("Wall showing recent anonymous contributions")]
public class SponsorWallSettings
{
    [Description("Section heading")]
    public string Heading { get; set; } = "";
}

[Description("Compact sponsor panel with contribution form and call-to-action")]
public class OneTimePaymentSettings
{
    [Description("Section heading")]
    public string Heading { get; set; } = "";
}
