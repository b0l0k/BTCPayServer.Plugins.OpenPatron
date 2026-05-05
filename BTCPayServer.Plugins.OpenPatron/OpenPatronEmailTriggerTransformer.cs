#nullable enable
using System.Linq;
using BTCPayServer.Abstractions;
using BTCPayServer.Data;
using BTCPayServer.Plugins.Emails;
using BTCPayServer.Plugins.Emails.Views;
using BTCPayServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Plugins.OpenPatron;

public class OpenPatronEmailTriggerTransformer(
    ApplicationDbContextFactory dbContextFactory,
    LinkGenerator linkGenerator,
    ISettingsAccessor<ServerSettings> serverSettings) : IEmailTriggerViewModelTransformer, IEmailTriggerEventTransformer
{
    public const string PortalUrlDoc = "The subscriber portal URL for the OpenPatron sponsoring page";
    public static string[] TranslatedStrings => [PortalUrlDoc];

    public void Transform(EmailTriggerViewModel viewModel)
    {
        if (serverSettings.Settings.BaseUrl != null && !viewModel.ServerTrigger)
        {
            viewModel.PlaceHolders.Add(new("{OpenPatron.PortalUrl}", PortalUrlDoc));
        }
    }

    public void Transform(IEmailTriggerEventTransformer.Context context)
    {
        if (serverSettings.Settings.BaseUrl is not { } baseUrl
            || !RequestBaseUrl.TryFromUrl(baseUrl, out var requestBase))
            return;

        var appId = context.TriggerEvent.Model["Offering"]?["AppId"]?.Value<string>();
        var offeringId = context.TriggerEvent.Model["Offering"]?["Id"]?.Value<string>();
        if (appId is null || offeringId is null)
            return;

        var subscriberEmail = context.TriggerEvent.Model["Subscriber"]?["Email"]?.Value<string>();
        if (string.IsNullOrEmpty(subscriberEmail))
            return;

        using var ctx = dbContextFactory.CreateContext();
        var app = ctx.Apps.Find(appId);
        if (app is null || app.AppType != OpenPatronAppType.AppType)
            return;

        var subscriberId = ctx.Subscribers
            .Include(s => s.Customer).ThenInclude(c => c.CustomerIdentities)
            .Where(s => s.OfferingId == offeringId
                        && s.Customer.CustomerIdentities.Any(ci => ci.Type == "Email" && ci.Value == subscriberEmail))
            .Select(s => (long?)s.Id)
            .FirstOrDefault();
        if (subscriberId is null)
            return;

        var portalUrl = linkGenerator.OpenPatronPortalLink(appId, subscriberId.Value, requestBase);
        var openPatronObj = (JObject)(context.TriggerEvent.Model["OpenPatron"] ??= new JObject());
        openPatronObj["PortalUrl"] = portalUrl;
    }
}
