using System.Collections.Generic;
using System.Linq;
using BTCPayServer.Data.Subscriptions;

namespace BTCPayServer.Plugins.OpenPatron;

public static class OpenPatronOfferingResolver
{
    public static OfferingData? SelectPreferredOffering(IEnumerable<OfferingData> offerings, string? preferredOfferingId)
    {
        var candidates = offerings.ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var best = candidates
            .OrderByDescending(o => o.Plans?.Count ?? 0)
            .ThenByDescending(o => o.Features?.Count ?? 0)
            .ThenByDescending(o => o.CreatedAt)
            .First();

        if (string.IsNullOrWhiteSpace(preferredOfferingId))
        {
            return best;
        }

        var preferred = candidates.FirstOrDefault(o => o.Id == preferredOfferingId);
        if (preferred is null)
        {
            return best;
        }

        var preferredPlanCount = preferred.Plans?.Count ?? 0;
        var preferredFeatureCount = preferred.Features?.Count ?? 0;
        var bestPlanCount = best.Plans?.Count ?? 0;
        var bestFeatureCount = best.Features?.Count ?? 0;
        var preferredHasStructure = preferredPlanCount > 0 || preferredFeatureCount > 0;
        var bestIsRicher = bestPlanCount > preferredPlanCount ||
                           (bestPlanCount == preferredPlanCount && bestFeatureCount > preferredFeatureCount);

        return preferredHasStructure || !bestIsRicher ? preferred : best;
    }
}