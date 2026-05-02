using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Controllers;
using BTCPayServer.Data;
using BTCPayServer.Data.Subscriptions;
using BTCPayServer.Plugins.OpenPatron.Models;
using BTCPayServer.Plugins.OpenPatron.Services;
using BTCPayServer.Plugins.OpenPatron.ViewModels;
using BTCPayServer.Plugins.Subscriptions;
using BTCPayServer.Services;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Plugins.OpenPatron.Controllers;

[Route("apps")]
[Area(OpenPatronPlugin.Area)]
public class UIOpenPatronController(
    AppService appService,
    ApplicationDbContextFactory dbContextFactory,
    UIInvoiceController uiInvoiceController,
    IAuthorizationService authorizationService,
    SponsorWallService sponsorWallService,
    FundingProgressService fundingProgressService,
    GitHubRepoService gitHubRepoService) : Controller
{
    private const string UpdateViewPath = "/Views/UIOpenPatron/Update.cshtml";
    private const string PublicPageViewPath = "/Views/UIOpenPatron/PublicPage.cshtml";

    [Authorize(Policy = Policies.CanViewStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [HttpGet("{appId}/settings/openpatron")]
    public async Task<IActionResult> Update(string appId)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
            return NotFound();

        if (!await IsAuthorized(app, Policies.CanViewStoreSettings))
            return Forbid();

        var settings = app.GetSettings<OpenPatronAppSettings>();
        EnsurePageLayout(settings);

        var offering = await GetOffering(app, settings);
        var vm = ToUpdateViewModel(app, settings, offering);

        return View(UpdateViewPath, vm);
    }

    [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [HttpPost("{appId}/settings/openpatron")]
    public async Task<IActionResult> Update(string appId, UpdateOpenPatronViewModel viewModel)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
            return NotFound();

        if (!await IsAuthorized(app, Policies.CanModifyStoreSettings))
            return Forbid();

        var existingSettings = app.GetSettings<OpenPatronAppSettings>();

        if (!existingSettings.PageTypeConfirmed)
        {
            existingSettings.PageType = viewModel.PageType;
            existingSettings.PageTypeConfirmed = true;
            existingSettings.PageLayout = BlockRegistry.DefaultLayoutFor(viewModel.PageType);
            existingSettings.Theme = new PageTheme();
            app.SetSettings(existingSettings);
            await appService.UpdateOrCreateApp(app);
            TempData[WellKnownTempData.SuccessMessage] = "Template selected. You can now configure your page.";
            return RedirectToAction(nameof(Update), new { appId = app.Id });
        }

        if (!ModelState.IsValid)
        {
            viewModel.AppId = app.Id;
            viewModel.StoreId = app.StoreDataId;
            viewModel.PublicPageUrl = GetPublicPageUrl(app.Id);
            return View(UpdateViewPath, viewModel);
        }

        List<BlockDefinition> pageLayout;
        try
        {
            pageLayout = JsonConvert.DeserializeObject<List<BlockDefinition>>(viewModel.PageLayoutJson ?? "[]") ?? [];
            pageLayout = pageLayout.Where(b => BlockRegistry.IsKnownType(b.Type)).ToList();
        }
        catch
        {
            pageLayout = existingSettings.PageLayout ?? [];
        }

        var settings = new OpenPatronAppSettings
        {
            PageType = existingSettings.PageType,
            PageTypeConfirmed = true,
            PageLayout = pageLayout,
            Theme = new PageTheme
            {
                AccentColor = NormalizeString(viewModel.ThemeAccentColor) ?? "#6366f1",
                BorderRadius = NormalizeString(viewModel.ThemeBorderRadius) ?? "1.5rem",
                BlockSpacing = NormalizeString(viewModel.ThemeBlockSpacing) ?? "1rem",
            },
            OfferingId = viewModel.OfferingId,
            SupportMode = viewModel.SupportMode,
            DefaultCurrency = viewModel.DefaultCurrency.Trim().ToUpperInvariant(),
            Visibility = viewModel.Visibility,
            AccentColor = NormalizeString(viewModel.ThemeAccentColor),
            PrimaryCallToActionUrl = existingSettings.PrimaryCallToActionUrl,
            Links = existingSettings.Links
        };

        settings.OfferingId = await ResolveOfferingId(
            app,
            viewModel.OfferingId,
            createIfMissing: AllowsSubscriptions(settings));

        app.Name = viewModel.AppName.Trim();
        app.SetSettings(settings);
        await appService.UpdateOrCreateApp(app);
        TempData[WellKnownTempData.SuccessMessage] = "OpenPatron page updated.";

        return RedirectToAction(nameof(Update), new { appId = app.Id });
    }

    [AllowAnonymous]
    [HttpGet("{appId}/openpatron")]
    public async Task<IActionResult> PublicPage(string appId)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
            return NotFound();

        var settings = app.GetSettings<OpenPatronAppSettings>();
        if (!IsPublished(settings) && !await IsAuthorized(app, Policies.CanViewStoreSettings))
            return NotFound();

        EnsurePageLayout(settings);

        var offering = AllowsSubscriptions(settings) ? await GetOffering(app, settings) : null;
        var vm = ToPublicViewModel(app, settings, offering);

        // Scan blocks for runtime data needs
        var fundingBlock = settings.PageLayout?.FirstOrDefault(b =>
            string.Equals(b.Type, BlockRegistry.FundingProgress, StringComparison.OrdinalIgnoreCase));
        if (fundingBlock != null)
        {
            var goal = BlockSettingsHelper.Dec(fundingBlock.Settings, "goal");
            if (goal is > 0)
            {
                vm.AmountRaised = await fundingProgressService.GetTotalRaisedAsync(app.Id, settings.DefaultCurrency);
                vm.FundingPercentage = (int)Math.Min(100, Math.Round(vm.AmountRaised / goal.Value * 100));
            }
        }

        var hasSponsorWall = settings.PageLayout?.Any(b =>
            string.Equals(b.Type, BlockRegistry.SponsorWall, StringComparison.OrdinalIgnoreCase)) ?? false;
        if (hasSponsorWall)
        {
            var entries = await sponsorWallService.GetRecentContributionsAsync(app.Id);
            vm.SponsorWallEntries = entries.Select(e => new SponsorWallEntryViewModel
            {
                Timestamp = e.Timestamp,
                Amount = e.Amount,
                Currency = e.Currency
            }).ToList();
        }

        await EnrichGitHubProjectsAsync(settings);

        return View(PublicPageViewPath, vm);
    }

    [AllowAnonymous]
    [HttpPost("{appId}/openpatron/contribute")]
    public async Task<IActionResult> Contribute(string appId, string amount)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
            return NotFound();

        var settings = app.GetSettings<OpenPatronAppSettings>();
        if (!AllowsOneTime(settings))
            return NotFound();

        if (!IsPublished(settings) && !await IsAuthorized(app, Policies.CanViewStoreSettings))
            return NotFound();

        if (!decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) || parsedAmount <= 0m)
        {
            TempData[WellKnownTempData.ErrorMessage] = "Please enter a valid amount greater than 0.";
            return RedirectToAction(nameof(PublicPage), new { appId });
        }

        try
        {
            var store = await appService.GetStore(app);
            var invoice = await uiInvoiceController.CreateInvoiceCoreRaw(
                new CreateInvoiceRequest
                {
                    Amount = parsedAmount,
                    Currency = settings.DefaultCurrency,
                    Metadata = new InvoiceMetadata
                    {
                        OrderId = AppService.GetRandomOrderId(),
                        ItemCode = "openpatron-onetime",
                        ItemDesc = $"{app.Name} - one-time contribution",
                        OrderUrl = GetPublicPageUrl(app.Id)
                    }.ToJObject(),
                    Checkout = new CreateInvoiceRequest.CheckoutOptions
                    {
                        RedirectURL = GetPublicPageUrl(app.Id)
                    },
                    AdditionalSearchTerms = [AppService.GetAppSearchTerm(app)]
                },
                store,
                HttpContext.Request.GetAbsoluteRoot(),
                [AppService.GetAppInternalTag(app.Id)]);

            return RedirectToAction(nameof(UIInvoiceController.Checkout), "UIInvoice", new { invoiceId = invoice.Id });
        }
        catch (BitpayHttpException ex)
        {
            TempData[WellKnownTempData.ErrorMessage] = ex.Message;
            return RedirectToAction(nameof(PublicPage), new { appId });
        }
    }

    [AllowAnonymous]
    [HttpPost("{appId}/openpatron/plans/{planId}/subscribe")]
    public async Task<IActionResult> Subscribe(string appId, string planId, bool isTrial = false)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
            return NotFound();

        var settings = app.GetSettings<OpenPatronAppSettings>();
        if (!AllowsSubscriptions(settings))
            return NotFound();

        if (!IsPublished(settings) && !await IsAuthorized(app, Policies.CanViewStoreSettings))
            return NotFound();

        var offering = await GetOffering(app, settings);
        var plan = offering?.Plans.FirstOrDefault(p => p.Id == planId && p.Status == PlanData.PlanStatus.Active);
        if (plan is null)
            return NotFound();

        await using var ctx = dbContextFactory.CreateContext();
        var checkout = new PlanCheckoutData()
        {
            PlanId = plan.Id,
            NewSubscriber = true,
            IsTrial = plan.TrialDays > 0 && isTrial,
            SuccessRedirectUrl = GetPublicPageUrl(app.Id),
            BaseUrl = Request.GetRequestBaseUrl(),
            Expiration = DateTimeOffset.UtcNow.AddDays(1),
            InvoiceMetadata = new InvoiceMetadata()
            {
                OrderId = AppService.GetRandomOrderId(),
                ItemCode = plan.Id,
                ItemDesc = $"{app.Name} - {plan.Name}",
                OrderUrl = GetPublicPageUrl(app.Id)
            }.ToJObject().ToString()
        };

        ctx.PlanCheckouts.Add(checkout);
        await ctx.SaveChangesAsync();

        return RedirectToAction("PlanCheckout", "UIPlanCheckout", new { area = SubscriptionsPlugin.Area, checkoutId = checkout.Id });
    }

    [AllowAnonymous]
    [HttpGet("openpatron/schema.json")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Schema()
    {
        var schema = new
        {
            Schema = "https://json-schema.org/draft/2020-12/schema",
            Title = "OpenPatron Page Layout",
            Type = "object",
            Properties = new Dictionary<string, object>
            {
                ["theme"] = new
                {
                    Type = "object",
                    Properties = new Dictionary<string, object>
                    {
                        ["AccentColor"] = new { Type = "string", Description = "CSS color value (e.g. #6366f1)", Pattern = "^#[0-9a-fA-F]{6}$" },
                        ["BorderRadius"] = new { Type = "string", Description = "CSS border-radius (e.g. 1.5rem)" },
                        ["BlockSpacing"] = new { Type = "string", Description = "CSS spacing between blocks (e.g. 1rem)" }
                    }
                },
                ["blocks"] = new
                {
                    Type = "array",
                    Items = new
                    {
                        Type = "object",
                        Required = new[] { "Id", "Type" },
                        Properties = new Dictionary<string, object>
                        {
                            ["Id"] = new { Type = "string", Description = "Unique block identifier" },
                            ["Type"] = new { Type = "string", Enum = BlockRegistry.AllTypes.Keys.ToArray(), Description = "Block type" },
                            ["Settings"] = new { Type = "object", Description = "Block-specific content settings" },
                            ["Theme"] = new
                            {
                                Type = "object",
                                Description = "Per-block theme overrides (inherits from global theme if omitted)",
                                Properties = new Dictionary<string, object>
                                {
                                    ["AccentColor"] = new { Type = "string", Description = "Override accent color for this block" },
                                    ["BorderRadius"] = new { Type = "string", Description = "Override border radius for this block" }
                                }
                            }
                        }
                    }
                }
            }
        };

        return Json(schema);
    }

    public static void EnsurePageLayout(OpenPatronAppSettings settings)
    {
        settings.PageLayout ??= [];
        settings.Theme ??= new PageTheme();
    }

    private async Task<bool> IsAuthorized(AppData app, string policy)
        => (await authorizationService.AuthorizeAsync(User, app.StoreDataId, policy)).Succeeded;

    private UpdateOpenPatronViewModel ToUpdateViewModel(AppData app, OpenPatronAppSettings settings, OfferingData? offering)
    {
        var theme = settings.Theme ?? new PageTheme();

        return new UpdateOpenPatronViewModel
        {
            AppId = app.Id,
            StoreId = app.StoreDataId,
            PublicPageUrl = GetPublicPageUrl(app.Id),
            OfferingId = offering?.Id ?? settings.OfferingId,
            ManageOfferingUrl = offering is null ? null : GetManageOfferingUrl(app.StoreDataId, offering.Id),
            AddPlanUrl = offering is null ? null : GetAddPlanUrl(app.StoreDataId, offering.Id),
            ActivePlanCount = offering?.Plans.Count(p => p.Status == PlanData.PlanStatus.Active) ?? 0,
            Archived = app.Archived,
            PageType = settings.PageType,
            PageTypeConfirmed = settings.PageTypeConfirmed,
            PageLayoutJson = JsonConvert.SerializeObject(settings.PageLayout ?? [], Formatting.None),
            ThemeAccentColor = theme.AccentColor,
            ThemeBorderRadius = theme.BorderRadius,
            ThemeBlockSpacing = theme.BlockSpacing,
            SupportMode = settings.SupportMode,
            AppName = app.Name,
            DefaultCurrency = settings.DefaultCurrency,
            Visibility = settings.Visibility
        };
    }

    private OpenPatronPublicViewModel ToPublicViewModel(AppData app, OpenPatronAppSettings settings, OfferingData? offering)
    {
        var theme = settings.Theme ?? new PageTheme();

        return new OpenPatronPublicViewModel
        {
            AppId = app.Id,
            AppName = app.Name,
            PublicPageUrl = GetPublicPageUrl(app.Id),
            SupportsOneTime = AllowsOneTime(settings),
            SupportsSubscriptions = AllowsSubscriptions(settings),
            DefaultCurrency = settings.DefaultCurrency,
            PageLayout = settings.PageLayout ?? [],
            Theme = theme,
            Plans = offering?.Plans
                .Where(plan => plan.Status == PlanData.PlanStatus.Active)
                .OrderBy(plan => plan.Price)
                .Select(plan => new OpenPatronPublicPlanViewModel
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Description = plan.Description ?? string.Empty,
                    Price = $"{plan.Currency} {plan.Price:0.##}",
                    BillingPeriod = plan.RecurringType switch
                    {
                        PlanData.RecurringInterval.Monthly => "per month",
                        PlanData.RecurringInterval.Quarterly => "per quarter",
                        PlanData.RecurringInterval.Yearly => "per year",
                        PlanData.RecurringInterval.Lifetime => "one time",
                        _ => plan.RecurringType.ToString()
                    },
                    SubscribeUrl = Url.Action(nameof(Subscribe), new { appId = app.Id, planId = plan.Id, area = OpenPatronPlugin.Area })
                                   ?? $"/apps/{app.Id}/openpatron/plans/{plan.Id}/subscribe",
                    HasTrial = plan.TrialDays > 0,
                    TrialLabel = plan.TrialDays > 0 ? $"{plan.TrialDays} day trial" : null
                })
                .ToList()
                ?? []
        };
    }

    private async Task<OfferingData?> GetOffering(AppData app, OpenPatronAppSettings settings)
    {
        await using var ctx = dbContextFactory.CreateContext();
        var offerings = await ctx.Offerings
            .IncludeAll()
            .Where(o => o.AppId == app.Id && o.App.StoreDataId == app.StoreDataId)
            .ToListAsync();
        await ctx.Plans.FetchPlanFeaturesAsync(offerings.SelectMany(o => o.Plans).ToArray());
        return OpenPatronOfferingResolver.SelectPreferredOffering(offerings, settings.OfferingId);
    }

    private async Task<string?> ResolveOfferingId(AppData app, string? preferredOfferingId, bool createIfMissing)
    {
        await using var ctx = dbContextFactory.CreateContext();
        var offerings = await ctx.Offerings
            .IncludeAll()
            .Where(o => o.AppId == app.Id && o.App.StoreDataId == app.StoreDataId)
            .ToListAsync();

        var existing = OpenPatronOfferingResolver.SelectPreferredOffering(offerings, preferredOfferingId);
        if (existing is not null || !createIfMissing)
            return existing?.Id;

        var offering = new OfferingData { AppId = app.Id };
        ctx.Offerings.Add(offering);
        await ctx.SaveChangesAsync();
        return offering.Id;
    }

    private string GetPublicPageUrl(string appId)
        => Url.ActionLink(nameof(PublicPage), values: new { appId, area = OpenPatronPlugin.Area }) ?? $"/apps/{appId}/openpatron";

    private string GetManageOfferingUrl(string storeId, string offeringId)
        => Url.Action("Offering", "UIOffering", new { area = SubscriptionsPlugin.Area, storeId, offeringId, section = "Plans" })
           ?? $"/stores/{storeId}/offerings/{offeringId}/Plans";

    private string GetAddPlanUrl(string storeId, string offeringId)
        => Url.Action("AddPlan", "UIOffering", new { area = SubscriptionsPlugin.Area, storeId, offeringId })
           ?? $"/stores/{storeId}/offerings/{offeringId}/add-plan";

    private async Task EnrichGitHubProjectsAsync(OpenPatronAppSettings settings)
    {
        var projectsBlocks = settings.PageLayout?
            .Where(b => string.Equals(b.Type, BlockRegistry.ProjectsGrid, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (projectsBlocks is not { Count: > 0 })
            return;

        foreach (var block in projectsBlocks)
        {
            var projects = BlockSettingsHelper.Arr(block.Settings, "projects");
            if (projects is null)
                continue;

            foreach (var token in projects.OfType<JObject>())
            {
                var url = BlockSettingsHelper.Str(token, "url");
                if (!GitHubRepoService.TryParseGitHubUrl(url, out var owner, out var repo))
                    continue;

                var ghRepo = await gitHubRepoService.GetRepoAsync(owner, repo);
                if (ghRepo is null)
                    continue;

                token["stars"] = ghRepo.StargazersCount;
                if (!string.IsNullOrWhiteSpace(ghRepo.Language))
                    token["language"] = ghRepo.Language;
            }
        }
    }

    private static bool AllowsOneTime(OpenPatronAppSettings settings)
        => settings.SupportMode is OpenPatronSupportMode.OneTimeOnly or OpenPatronSupportMode.Both;

    private static bool AllowsSubscriptions(OpenPatronAppSettings settings)
        => settings.SupportMode is OpenPatronSupportMode.SubscriptionOnly or OpenPatronSupportMode.Both;

    private static bool IsPublished(OpenPatronAppSettings settings)
        => settings.Visibility == OpenPatronVisibility.Published;

    private static string? NormalizeString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? ComputeGravatarUrl(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var hash = ComputeMd5Hash(email.Trim().ToLowerInvariant());
        return $"https://www.gravatar.com/avatar/{hash}?s=200&d=identicon";
    }

    public static string ComputeMd5Hash(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(32);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
