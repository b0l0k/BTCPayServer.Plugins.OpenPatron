using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Services.Apps;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.OpenPatron.Services;

public class SponsorWallService(ApplicationDbContextFactory dbContextFactory)
{
    public async Task<List<SponsorWallEntry>> GetRecentContributionsAsync(
        string appId,
        int limit = 50,
        CancellationToken ct = default)
    {
        var searchTerm = AppService.GetAppSearchTerm(OpenPatronAppType.AppType, appId);

        await using var ctx = dbContextFactory.CreateContext();
        var entries = await ctx.Invoices
            .Where(i => i.InvoiceSearchData.Any(s => s.Value == searchTerm) &&
                        i.Status == InvoiceData.Settled)
            .OrderByDescending(i => i.Created)
            .Take(limit)
            .Select(i => new SponsorWallEntry
            {
                Timestamp = i.Created,
                Amount = i.Amount ?? 0m,
                Currency = i.Currency ?? "USD"
            })
            .ToListAsync(ct);

        return entries;
    }
}

public class SponsorWallEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}
