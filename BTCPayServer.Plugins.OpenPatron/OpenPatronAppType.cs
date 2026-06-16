using System.Threading.Tasks;
using BTCPayServer.Configuration;
using BTCPayServer.Data;
using BTCPayServer.Plugins.OpenPatron.Controllers;
using BTCPayServer.Plugins.OpenPatron.Models;
using BTCPayServer.Services.Apps;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace BTCPayServer.Plugins.OpenPatron;

public class OpenPatronAppType : AppBaseType
{
    private readonly LinkGenerator linkGenerator;
    private readonly IOptions<BTCPayServerOptions> btcPayServerOptions;

    public OpenPatronAppType(
        LinkGenerator linkGenerator, 
        IOptions<BTCPayServerOptions> btcPayServerOptions) : base(AppType)
    {
        this.linkGenerator = linkGenerator;
        this.btcPayServerOptions = btcPayServerOptions;
        Description = "Open Patron";
    }

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
            DefaultCurrency = defaultCurrency
        });

        return Task.CompletedTask;
    }
}
