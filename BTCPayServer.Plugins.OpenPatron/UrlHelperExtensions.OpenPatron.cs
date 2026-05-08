#nullable enable
using BTCPayServer.Abstractions;
using BTCPayServer.Plugins.OpenPatron;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc;

public static class OpenPatronUrlHelperExtensions
{
    public static string OpenPatronPortalLink(this LinkGenerator linkGenerator, string appId, string customerId, RequestBaseUrl requestBaseUrl)
        => linkGenerator.GetUriByAction(
            action: "SubscriberPortalRedirect",
            controller: "UIOpenPatron",
            values: new { area = OpenPatronPlugin.Area, appId, customerId },
            requestBaseUrl: requestBaseUrl);
}
