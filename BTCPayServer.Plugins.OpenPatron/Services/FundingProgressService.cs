using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Services.Apps;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.OpenPatron.Services;

public class FundingProgressService(ApplicationDbContextFactory dbContextFactory)
{
    public async Task<decimal> GetTotalRaisedAsync(
        string appId,
        string currency,
        CancellationToken ct = default)
    {
        var searchTerm = AppService.GetAppSearchTerm(OpenPatronAppType.AppType, appId);

        await using var ctx = dbContextFactory.CreateContext();
        var total = await ctx.Invoices
            .Where(i => i.InvoiceSearchData.Any(s => s.Value == searchTerm) &&
                        i.Status == InvoiceData.Settled &&
                        i.Currency == currency)
            .SumAsync(i => i.Amount ?? 0m, ct);

        return total;
    }
}
