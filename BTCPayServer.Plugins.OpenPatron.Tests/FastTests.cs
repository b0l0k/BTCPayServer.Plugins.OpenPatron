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
