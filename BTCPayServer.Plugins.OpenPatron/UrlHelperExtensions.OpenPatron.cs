#nullable enable
using BTCPayServer.Abstractions;
using BTCPayServer.Plugins.OpenPatron;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc;

public static class OpenPatronUrlHelperExtensions
{
    public static string OpenPatronPortalLink(this LinkGenerator linkGenerator, string appId, long subscriberId, RequestBaseUrl requestBaseUrl)
        => linkGenerator.GetUriByAction(
            action: "SubscriberPortalRedirect",
            controller: "UIOpenPatron",
            values: new { area = OpenPatronPlugin.Area, appId, subscriberId },
            requestBaseUrl: requestBaseUrl);
}
