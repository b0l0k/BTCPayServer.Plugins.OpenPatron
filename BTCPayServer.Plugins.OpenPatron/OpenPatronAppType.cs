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
            PageType = OpenPatronPageType.Project,
            PageTypeConfirmed = false,
            DefaultCurrency = defaultCurrency,
            Visibility = OpenPatronVisibility.Unpublished
        });

        return Task.CompletedTask;
    }
}
