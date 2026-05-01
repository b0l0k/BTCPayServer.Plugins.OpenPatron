using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.OpenPatron.Services;
using BTCPayServer.Services.Apps;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.OpenPatron;

public class OpenPatronPlugin : BaseBTCPayServerPlugin
{
    public const string Area = "OpenPatron";

    public override string Identifier => "BTCPayServer.Plugins.OpenPatron";
    public override string Name => "OpenPatron";
    public override string Description => "Host sponsor pages for open source maintainers with BTCPay Server apps.";

    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    [
        new IBTCPayServerPlugin.PluginDependency
        {
            Identifier = nameof(BTCPayServer),
            Condition = ">=2.0.0"
        }
    ];

    public override void Execute(IServiceCollection services)
    {
        services.AddSingleton<AppBaseType, OpenPatronAppType>();
        services.AddUIExtension("header-nav", "OpenPatron/NavExtension");
        services.AddHttpClient("GitHub");
        services.AddTransient<GitHubRepoService>();
        services.AddTransient<SponsorWallService>();
        services.AddTransient<FundingProgressService>();
        services.AddStartupTask<OpenPatronResetStartupTask>();

        base.Execute(services);
    }
}
