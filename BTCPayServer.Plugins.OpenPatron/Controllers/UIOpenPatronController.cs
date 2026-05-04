using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using NJsonSchema;
using NJsonSchema.Generation;

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
            existingSettings.Sections = BlockRegistry.DefaultSectionsFor(viewModel.PageType);
            existingSettings.PageLayoutPreset = "8-4";
            existingSettings.Theme = new PageTheme();
            existingSettings.PageLayout = null;
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

        List<PageSection> sections;
        try
        {
            sections = JsonConvert.DeserializeObject<List<PageSection>>(viewModel.SectionsJson ?? "[]") ?? [];
            foreach (var section in sections)
                section.Blocks = section.Blocks.Where(b => BlockRegistry.IsKnownType(b.Type)).ToList();
        }
        catch
        {
            sections = existingSettings.Sections ?? BlockRegistry.CreateSectionsForPreset(viewModel.PageLayoutPreset);
        }

        var settings = new OpenPatronAppSettings
        {
            PageType = existingSettings.PageType,
            PageTypeConfirmed = true,
            PageLayoutPreset = viewModel.PageLayoutPreset,
            Sections = sections,
            Theme = new PageTheme
            {
                AccentColor = NormalizeString(viewModel.ThemeAccentColor) ?? PageTheme.DefaultAccentColor,
                BorderRadius = NormalizeString(viewModel.ThemeBorderRadius) ?? "1.5rem",
                BlockSpacing = NormalizeString(viewModel.ThemeBlockSpacing) ?? "1rem",
            },
            OfferingId = viewModel.OfferingId,
            SupportMode = viewModel.SupportMode,
            DefaultCurrency = viewModel.DefaultCurrency.Trim().ToUpperInvariant(),
            Visibility = viewModel.Visibility
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
        var allBlocks = BlockRegistry.AllBlocks(settings.Sections);
        var fundingBlock = allBlocks.FirstOrDefault(b =>
            string.Equals(b.Type, BlockRegistry.FundingProgress, StringComparison.OrdinalIgnoreCase));
        if (fundingBlock != null)
        {
            var goal = fundingBlock.Settings?.ToObject<FundingProgressSettings>()?.Goal;
            if (goal is > 0)
            {
                vm.AmountRaised = await fundingProgressService.GetTotalRaisedAsync(app.Id, settings.DefaultCurrency);
                vm.FundingPercentage = (int)Math.Min(100, Math.Round(vm.AmountRaised / goal.Value * 100));
            }
        }

        var hasSponsorWall = allBlocks.Any(b =>
            string.Equals(b.Type, BlockRegistry.SponsorWall, StringComparison.OrdinalIgnoreCase));
        if (hasSponsorWall)
        {
            vm.SponsorWallEntries = await sponsorWallService.GetRecentContributionsAsync(app.Id);
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
        var generatorSettings = new SystemTextJsonSchemaGeneratorSettings();
        var generator = new JsonSchemaGenerator(generatorSettings);

        var blockSettingsSchemas = new JObject();
        foreach (var (typeKey, info) in BlockRegistry.AllTypes)
        {
            var typeSchema = generator.Generate(info.SettingsType);
            blockSettingsSchemas[typeKey] = JObject.Parse(typeSchema.ToJson());
        }

        var themeSchema = JObject.Parse(generator.Generate(typeof(PageTheme)).ToJson());
        var blockThemeSchema = JObject.Parse(generator.Generate(typeof(BlockTheme)).ToJson());

        var blockItemSchema = new JObject
        {
            ["type"] = "object",
            ["required"] = new JArray("Id", "Type"),
            ["properties"] = new JObject
            {
                ["Id"] = new JObject { ["type"] = "string", ["description"] = "Unique block identifier (12-char hex)" },
                ["Type"] = new JObject { ["type"] = "string", ["description"] = "Block type identifier", ["enum"] = new JArray(BlockRegistry.AllTypes.Keys.ToArray()) },
                ["Settings"] = new JObject
                {
                    ["description"] = "Block content settings (schema depends on Type)",
                    ["oneOf"] = new JArray(blockSettingsSchemas.Properties().Select(kv => new JObject
                    {
                        ["if"] = new JObject { ["properties"] = new JObject { ["Type"] = new JObject { ["const"] = kv.Name } } },
                        ["then"] = kv.Value
                    }))
                },
                ["Theme"] = blockThemeSchema
            }
        };

        var schema = new JObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = "OpenPatron Page Layout",
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["Theme"] = themeSchema,
                ["Layout"] = new JObject
                {
                    ["type"] = "string",
                    ["description"] = "Page layout preset",
                    ["enum"] = new JArray(BlockRegistry.LayoutPresets.Keys.ToArray())
                },
                ["Sections"] = new JObject
                {
                    ["type"] = "array",
                    ["description"] = "Page sections (columns), each containing blocks",
                    ["items"] = new JObject
                    {
                        ["type"] = "object",
                        ["required"] = new JArray("Id", "Width", "Blocks"),
                        ["properties"] = new JObject
                        {
                            ["Id"] = new JObject { ["type"] = "string", ["description"] = "Section identifier (e.g. col-1, col-2)" },
                            ["Width"] = new JObject { ["type"] = "integer", ["description"] = "Bootstrap column width (4, 6, 8, or 12)", ["enum"] = new JArray(4, 6, 8, 12) },
                            ["Blocks"] = new JObject
                            {
                                ["type"] = "array",
                                ["description"] = "Ordered list of blocks in this section",
                                ["items"] = blockItemSchema
                            }
                        }
                    }
                }
            },
            ["$defs"] = new JObject
            {
                ["BlockSettings"] = blockSettingsSchemas
            }
        };

        return Json(schema);
    }

    [AllowAnonymous]
    [HttpGet("{appId}/openpatron/badge.svg")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> Badge(string appId, string style = "flat", string? label = null)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
            return NotFound();

        var settings = app.GetSettings<OpenPatronAppSettings>();
        if (!IsPublished(settings) && !await IsAuthorized(app, Policies.CanViewStoreSettings))
            return NotFound();

        var accentColor = settings.Theme?.AccentColor ?? PageTheme.DefaultAccentColor;
        var badgeLabel = string.IsNullOrWhiteSpace(label) ? "\u20bf" : label.Trim();
        var isSquare = string.Equals(style, "flat-square", StringComparison.OrdinalIgnoreCase);

        var svg = GenerateBadgeSvg(badgeLabel, "Sponsor", accentColor, isSquare);
        return Content(svg, "image/svg+xml; charset=utf-8");
    }

    public static void EnsurePageLayout(OpenPatronAppSettings settings)
    {
        settings.Theme ??= new PageTheme();

        if (settings.Sections is { Count: > 0 })
            return;

        // Migrate from flat PageLayout to section-based
        if (settings.PageLayout is { Count: > 0 })
        {
            var sections = BlockRegistry.CreateSectionsForPreset(settings.PageLayoutPreset);
            var widerSection = sections.OrderByDescending(s => s.Width).First();
            widerSection.Blocks = settings.PageLayout;

            // Add a default sidebar-support block to the narrower column if it exists
            var narrower = sections.FirstOrDefault(s => s != widerSection);
            if (narrower != null)
            {
                narrower.Blocks =
                [
                    new BlockDefinition
                    {
                        Type = BlockRegistry.SidebarSupport,
                        Settings = JObject.FromObject(new SidebarSupportSettings { Heading = "Sponsor now" })
                    }
                ];
            }

            settings.Sections = sections;
            settings.PageLayout = null;
        }
        else
        {
            settings.Sections = BlockRegistry.CreateSectionsForPreset(settings.PageLayoutPreset);
        }
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
            BadgeUrl = GetBadgeUrl(app.Id),
            OfferingId = offering?.Id ?? settings.OfferingId,
            ManageOfferingUrl = offering is null ? null : GetManageOfferingUrl(app.StoreDataId, offering.Id),
            AddPlanUrl = offering is null ? null : GetAddPlanUrl(app.StoreDataId, offering.Id),
            ActivePlanCount = offering?.Plans.Count(p => p.Status == PlanData.PlanStatus.Active) ?? 0,
            Archived = app.Archived,
            PageType = settings.PageType,
            PageTypeConfirmed = settings.PageTypeConfirmed,
            PageLayoutPreset = settings.PageLayoutPreset,
            SectionsJson = JsonConvert.SerializeObject(settings.Sections ?? [], Formatting.None),
            PageLayoutJson = JsonConvert.SerializeObject(settings.Sections ?? [], Formatting.None),
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
        var allBlocks = BlockRegistry.AllBlocks(settings.Sections).ToList();

        return new OpenPatronPublicViewModel
        {
            AppId = app.Id,
            AppName = app.Name,
            PublicPageUrl = GetPublicPageUrl(app.Id),
            SupportsOneTime = AllowsOneTime(settings),
            SupportsSubscriptions = AllowsSubscriptions(settings),
            DefaultCurrency = settings.DefaultCurrency,
            Sections = settings.Sections ?? [],
            PageLayout = allBlocks,
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

    private async Task<List<OfferingData>> GetOfferingsForApp(AppData app)
    {
        await using var ctx = dbContextFactory.CreateContext();
        var offerings = await ctx.Offerings
            .IncludeAll()
            .Where(o => o.AppId == app.Id && o.App.StoreDataId == app.StoreDataId)
            .ToListAsync();
        await ctx.Plans.FetchPlanFeaturesAsync(offerings.SelectMany(o => o.Plans).ToArray());
        return offerings;
    }

    private async Task<OfferingData?> GetOffering(AppData app, OpenPatronAppSettings settings)
    {
        var offerings = await GetOfferingsForApp(app);
        return OpenPatronOfferingResolver.SelectPreferredOffering(offerings, settings.OfferingId);
    }

    private async Task<string?> ResolveOfferingId(AppData app, string? preferredOfferingId, bool createIfMissing)
    {
        var offerings = await GetOfferingsForApp(app);
        var existing = OpenPatronOfferingResolver.SelectPreferredOffering(offerings, preferredOfferingId);
        if (existing is not null || !createIfMissing)
            return existing?.Id;

        await using var ctx = dbContextFactory.CreateContext();
        var offering = new OfferingData { AppId = app.Id };
        ctx.Offerings.Add(offering);
        await ctx.SaveChangesAsync();
        return offering.Id;
    }

    private string GetPublicPageUrl(string appId)
        => Url.ActionLink(nameof(PublicPage), values: new { appId, area = OpenPatronPlugin.Area }) ?? $"/apps/{appId}/openpatron";

    private string GetBadgeUrl(string appId)
        => Url.ActionLink(nameof(Badge), values: new { appId, area = OpenPatronPlugin.Area }) ?? $"/apps/{appId}/openpatron/badge.svg";

    private string GetManageOfferingUrl(string storeId, string offeringId)
        => Url.Action("Offering", "UIOffering", new { area = SubscriptionsPlugin.Area, storeId, offeringId, section = "Plans" })
           ?? $"/stores/{storeId}/offerings/{offeringId}/Plans";

    private string GetAddPlanUrl(string storeId, string offeringId)
        => Url.Action("AddPlan", "UIOffering", new { area = SubscriptionsPlugin.Area, storeId, offeringId })
           ?? $"/stores/{storeId}/offerings/{offeringId}/add-plan";

    private async Task EnrichGitHubProjectsAsync(OpenPatronAppSettings settings)
    {
        var projectsBlocks = BlockRegistry.AllBlocks(settings.Sections)
            .Where(b => string.Equals(b.Type, BlockRegistry.ProjectsGrid, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (projectsBlocks is not { Count: > 0 })
            return;

        foreach (var block in projectsBlocks)
        {
            var gridSettings = block.Settings?.ToObject<ProjectsGridSettings>();
            if (gridSettings?.Projects is not { Count: > 0 })
                continue;

            var projectTokens = BlockSettingsHelper.Arr(block.Settings, nameof(ProjectsGridSettings.Projects));
            if (projectTokens is null)
                continue;

            foreach (var token in projectTokens.OfType<JObject>())
            {
                var url = BlockSettingsHelper.Str(token, "Url");
                if (!GitHubRepoService.TryParseGitHubUrl(url, out var owner, out var repo))
                    continue;

                var ghRepo = await gitHubRepoService.GetRepoAsync(owner, repo);
                if (ghRepo is null)
                    continue;

                token["Stars"] = ghRepo.StargazersCount;
                if (!string.IsNullOrWhiteSpace(ghRepo.Language))
                    token["Language"] = ghRepo.Language;
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

    private static string GenerateBadgeSvg(string label, string message, string color, bool square)
    {
        const double charWidth = 6.8;
        const int sidePadding = 6;
        const int height = 20;

        var labelTextWidth = (int)Math.Ceiling(label.Length * charWidth);
        var messageTextWidth = (int)Math.Ceiling(message.Length * charWidth);
        var labelWidth = Math.Max(labelTextWidth + 2 * sidePadding, 24);
        var messageWidth = messageTextWidth + 2 * sidePadding;
        var totalWidth = labelWidth + messageWidth;

        var labelMid = (labelWidth / 2.0).ToString("F1", CultureInfo.InvariantCulture);
        var messageMid = (labelWidth + messageWidth / 2.0).ToString("F1", CultureInfo.InvariantCulture);
        var rx = square ? 0 : 3;

        label = EscapeXml(label);
        message = EscapeXml(message);
        color = EscapeXml(color);

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{totalWidth}" height="{height}">
              <linearGradient id="s" x2="0" y2="100%">
                <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
                <stop offset="1" stop-opacity=".1"/>
              </linearGradient>
              <clipPath id="r">
                <rect width="{totalWidth}" height="{height}" rx="{rx}" fill="#fff"/>
              </clipPath>
              <g clip-path="url(#r)">
                <rect width="{labelWidth}" height="{height}" fill="#555"/>
                <rect x="{labelWidth}" width="{messageWidth}" height="{height}" fill="{color}"/>
                <rect width="{totalWidth}" height="{height}" fill="url(#s)"/>
              </g>
              <g fill="#fff" text-anchor="middle" font-family="DejaVu Sans,Verdana,Geneva,sans-serif" text-rendering="geometricPrecision" font-size="11">
                <text x="{labelMid}" y="15" fill="#010101" fill-opacity=".3">{label}</text>
                <text x="{labelMid}" y="14">{label}</text>
                <text x="{messageMid}" y="15" fill="#010101" fill-opacity=".3">{message}</text>
                <text x="{messageMid}" y="14">{message}</text>
              </g>
            </svg>
            """;
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");

}
