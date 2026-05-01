using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BTCPayServer.Plugins.OpenPatron.Services;

/// <summary>
/// Deletes all existing OpenPatron apps on startup.
/// This is a dev-time convenience — remove once the schema stabilises.
/// </summary>
public class OpenPatronResetStartupTask(
    ApplicationDbContextFactory dbContextFactory,
    ILogger<OpenPatronResetStartupTask> logger) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = dbContextFactory.CreateContext();
        var apps = await ctx.Apps
            .Where(a => a.AppType == OpenPatronAppType.AppType)
            .ToListAsync(cancellationToken);

        if (apps.Count == 0)
            return;

        logger.LogInformation("OpenPatron reset: deleting {Count} existing app(s)", apps.Count);
        ctx.Apps.RemoveRange(apps);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
