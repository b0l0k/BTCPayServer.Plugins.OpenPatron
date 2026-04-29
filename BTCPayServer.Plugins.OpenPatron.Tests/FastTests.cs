using BTCPayServer.Plugins.OpenPatron.Controllers;
using BTCPayServer.Plugins.OpenPatron.Models;
using BTCPayServer.Data.Subscriptions;
using Xunit;

namespace BTCPayServer.Plugins.OpenPatron.Tests;

[Trait("Fast", "Fast")]
public class FastTests
{
    [Fact]
    public void OpenPatronSettingsDefaultsToUnpublished()
    {
        var settings = new OpenPatronAppSettings();

        Assert.Equal(OpenPatronVisibility.Unpublished, settings.Visibility);
        Assert.Equal(OpenPatronSupportMode.Both, settings.SupportMode);
        Assert.Null(settings.OfferingId);
        Assert.Empty(settings.SuggestedAmounts);
    }

    [Fact]
    public void DefaultPageTypeIsProject()
    {
        var settings = new OpenPatronAppSettings();
        Assert.Equal(OpenPatronPageType.Project, settings.PageType);
        Assert.False(settings.PageTypeConfirmed);
    }

    [Fact]
    public void AccentColorDefaultIsNull()
    {
        var settings = new OpenPatronAppSettings();
        Assert.Null(settings.AccentColor);
    }

    [Fact]
    public void SponsorWallDefaultDisabled()
    {
        var settings = new OpenPatronAppSettings();
        Assert.False(settings.ShowSponsorWall);
    }

    [Fact]
    public void SocialLinksDefaultNull()
    {
        var settings = new OpenPatronAppSettings();
        Assert.Null(settings.SocialLinks);
    }

    [Fact]
    public void FundingGoalDefaultNull()
    {
        var settings = new OpenPatronAppSettings();
        Assert.Null(settings.FundingGoal);
    }

    [Fact]
    public void GravatarUrlIsComputedFromEmail()
    {
        var url = UIOpenPatronController.ComputeGravatarUrl("test@example.com");
        Assert.NotNull(url);
        Assert.StartsWith("https://www.gravatar.com/avatar/", url);
        Assert.Contains("s=200", url);
        // MD5 of "test@example.com" is 55502f40dc8b7c769880b10874abc9d0
        Assert.Contains("55502f40dc8b7c769880b10874abc9d0", url);
    }

    [Fact]
    public void GravatarUrlIsNullForEmptyEmail()
    {
        Assert.Null(UIOpenPatronController.ComputeGravatarUrl(null));
        Assert.Null(UIOpenPatronController.ComputeGravatarUrl(""));
        Assert.Null(UIOpenPatronController.ComputeGravatarUrl("   "));
    }

    [Fact]
    public void Md5HashIsCorrect()
    {
        // Well-known MD5: "hello" -> 5d41402abc4b2a76b9719d911017c592
        var hash = UIOpenPatronController.ComputeMd5Hash("hello");
        Assert.Equal("5d41402abc4b2a76b9719d911017c592", hash);
    }

    [Fact]
    public void ProjectsListDefaultEmpty()
    {
        var settings = new OpenPatronAppSettings();
        Assert.Empty(settings.Projects);
    }

    [Fact]
    public void OfferingResolverPrefersRicherOfferingOverEmptyPreferred()
    {
        var richer = new OfferingData
        {
            Id = "offering-rich",
            CreatedAt = new(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            Plans = [new PlanData()],
            Features = []
        };
        var emptyPreferred = new OfferingData
        {
            Id = "offering-empty",
            CreatedAt = new(2026, 4, 23, 21, 0, 0, TimeSpan.Zero),
            Plans = [],
            Features = []
        };

        var selected = OpenPatronOfferingResolver.SelectPreferredOffering([richer, emptyPreferred], emptyPreferred.Id);

        Assert.Equal(richer.Id, selected?.Id);
    }

    [Fact]
    public void OfferingResolverKeepsPreferredOfferingWhenItHasPlans()
    {
        var olderPreferred = new OfferingData
        {
            Id = "offering-preferred",
            CreatedAt = new(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            Plans = [new PlanData()],
            Features = []
        };
        var newerEmpty = new OfferingData
        {
            Id = "offering-empty",
            CreatedAt = new(2026, 4, 23, 21, 0, 0, TimeSpan.Zero),
            Plans = [],
            Features = []
        };

        var selected = OpenPatronOfferingResolver.SelectPreferredOffering([olderPreferred, newerEmpty], olderPreferred.Id);

        Assert.Equal(olderPreferred.Id, selected?.Id);
    }
}
