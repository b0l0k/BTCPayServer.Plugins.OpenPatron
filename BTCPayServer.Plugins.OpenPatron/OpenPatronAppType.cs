using System.Collections.Generic;
using System.Threading.Tasks;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.Plugins.OpenPatron.Controllers;
using BTCPayServer.Plugins.OpenPatron.Models;
using BTCPayServer.Services.Apps;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace BTCPayServer.Plugins.OpenPatron;

public class OpenPatronAppType(
    LinkGenerator linkGenerator,
    IOptions<BTCPayServerOptions> btcPayServerOptions) : AppBaseType(AppType)
{
    public const string AppType = "OpenPatron";

    public override Task<object?> GetInfo(AppData appData)
        => Task.FromResult<object?>(appData.GetSettings<OpenPatronAppSettings>());

    public override Task<string> ConfigureLink(AppData app)
        => Task.FromResult(linkGenerator.GetPathByAction(
            action: nameof(UIOpenPatronController.Update),
            controller: "UIOpenPatron",
            values: new { area = OpenPatronPlugin.Area, appId = app.Id },
            pathBase: btcPayServerOptions.Value.RootPath) ?? $"/apps/{app.Id}/settings/openpatron");

    public override Task<string> ViewLink(AppData app)
        => Task.FromResult(linkGenerator.GetPathByAction(
            action: nameof(UIOpenPatronController.PublicPage),
            controller: "UIOpenPatron",
            values: new { area = OpenPatronPlugin.Area, appId = app.Id },
            pathBase: btcPayServerOptions.Value.RootPath) ?? $"/apps/{app.Id}/openpatron");

    public override Task SetDefaultSettings(AppData appData, string defaultCurrency)
    {
        appData.SetSettings(new OpenPatronAppSettings
        {
            SupportMode = OpenPatronSupportMode.Both,
            HeroTitle = appData.Name,
            HeroSubtitle = "Support ongoing maintenance and development.",
            Description = "Tell sponsors what you maintain, why it matters, and what their support unlocks.",
            DefaultCurrency = defaultCurrency,
            PrimaryCallToAction = "Sponsor this project",
            SuggestedAmounts = [5m, 15m, 50m],
            Visibility = OpenPatronVisibility.Unpublished,
            Links = new List<OpenPatronLink>
            {
                new() { Label = "Project", Url = string.Empty },
                new() { Label = "GitHub", Url = string.Empty }
            }
        });

        return Task.CompletedTask;
    }
}
