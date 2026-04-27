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
using BTCPayServer.Plugins.OpenPatron.ViewModels;
using BTCPayServer.Plugins.Subscriptions;
using BTCPayServer.Services;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.OpenPatron.Controllers;

[Route("apps")]
[Area(OpenPatronPlugin.Area)]
public class UIOpenPatronController(
    AppService appService,
    ApplicationDbContextFactory dbContextFactory,
    UIInvoiceController uiInvoiceController,
    IAuthorizationService authorizationService) : Controller
{
    private const string UpdateViewPath = "/Views/UIOpenPatron/Update.cshtml";
    private const string PublicPageViewPath = "/Views/UIOpenPatron/PublicPage.cshtml";

    [Authorize(Policy = Policies.CanViewStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [HttpGet("{appId}/settings/openpatron")]
    public async Task<IActionResult> Update(string appId)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
        {
            return NotFound();
        }

        if (!await IsAuthorized(app, Policies.CanViewStoreSettings))
        {
            return Forbid();
        }

        var settings = app.GetSettings<OpenPatronAppSettings>();
        var offering = await GetOffering(app, settings);
        return View(UpdateViewPath, ToUpdateViewModel(app, settings, offering));
    }

    [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [HttpPost("{appId}/settings/openpatron")]
    public async Task<IActionResult> Update(string appId, UpdateOpenPatronViewModel viewModel)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
        {
            return NotFound();
        }

        if (!await IsAuthorized(app, Policies.CanModifyStoreSettings))
        {
            return Forbid();
        }

        ValidateSuggestedAmounts(viewModel.SuggestedAmounts);

        if (!ModelState.IsValid)
        {
            viewModel.AppId = app.Id;
            viewModel.StoreId = app.StoreDataId;
            viewModel.PublicPageUrl = GetPublicPageUrl(app.Id);
            return View(UpdateViewPath, viewModel);
        }

        var settings = new OpenPatronAppSettings
        {
            OfferingId = viewModel.OfferingId,
            SupportMode = viewModel.SupportMode,
            HeroTitle = viewModel.HeroTitle.Trim(),
            HeroSubtitle = viewModel.HeroSubtitle.Trim(),
            Description = viewModel.Description.Trim(),
            PrimaryCallToAction = string.IsNullOrWhiteSpace(viewModel.PrimaryCallToAction)
                ? "Sponsor this project"
                : viewModel.PrimaryCallToAction.Trim(),
            PrimaryCallToActionUrl = NormalizeUrl(viewModel.PrimaryCallToActionUrl),
            DefaultCurrency = viewModel.DefaultCurrency.Trim().ToUpperInvariant(),
            SuggestedAmounts = ParseSuggestedAmounts(viewModel.SuggestedAmounts),
            Visibility = viewModel.Visibility,
            Links = (new OpenPatronLink?[]
            {
                CreateLink("Project", viewModel.ProjectUrl),
                CreateLink("GitHub", viewModel.GitHubUrl)
            }).Where(link => link is not null).Cast<OpenPatronLink>().ToList()
        };

        settings.OfferingId = await ResolveOfferingId(
            app,
            viewModel.OfferingId,
            createIfMissing: AllowsSubscriptions(settings));

        app.Name = viewModel.AppName.Trim();
        app.Archived = false;
        app.SetSettings(settings);

        await appService.UpdateOrCreateApp(app);
        TempData[WellKnownTempData.SuccessMessage] = "OpenPatron page updated and linked to Subscriptions.";

        return RedirectToAction(nameof(Update), new { appId = app.Id });
    }

    [AllowAnonymous]
    [HttpGet("{appId}/openpatron")]
    public async Task<IActionResult> PublicPage(string appId)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
        {
            return NotFound();
        }

        var settings = app.GetSettings<OpenPatronAppSettings>();
        var isPubliclyVisible = IsPublished(settings);
        if (!isPubliclyVisible && !await IsAuthorized(app, Policies.CanViewStoreSettings))
        {
            return NotFound();
        }

        var offering = AllowsSubscriptions(settings) ? await GetOffering(app, settings) : null;
        return View(PublicPageViewPath, ToPublicViewModel(app, settings, offering));
    }

    [AllowAnonymous]
    [HttpPost("{appId}/openpatron/contribute")]
    public async Task<IActionResult> Contribute(string appId, string amount)
    {
        var app = await appService.GetApp(appId, OpenPatronAppType.AppType, includeArchived: true);
        if (app is null)
        {
            return NotFound();
        }

        var settings = app.GetSettings<OpenPatronAppSettings>();
        if (!IsPublished(settings) || !AllowsOneTime(settings))
        {
            return NotFound();
        }

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
        {
            return NotFound();
        }

        var settings = app.GetSettings<OpenPatronAppSettings>();
        if (!IsPublished(settings))
        {
            return NotFound();
        }

        if (!AllowsSubscriptions(settings))
        {
            return NotFound();
        }

        var offering = await GetOffering(app, settings);
        var plan = offering?.Plans.FirstOrDefault(p => p.Id == planId && p.Status == PlanData.PlanStatus.Active);
        if (plan is null)
        {
            return NotFound();
        }

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

    private async Task<bool> IsAuthorized(AppData app, string policy)
        => (await authorizationService.AuthorizeAsync(User, app.StoreDataId, policy)).Succeeded;

    private UpdateOpenPatronViewModel ToUpdateViewModel(AppData app, OpenPatronAppSettings settings, OfferingData? offering)
    {
        return new UpdateOpenPatronViewModel
        {
            AppId = app.Id,
            StoreId = app.StoreDataId,
            PublicPageUrl = GetPublicPageUrl(app.Id),
            OfferingId = offering?.Id ?? settings.OfferingId,
            ManageOfferingUrl = offering is null ? null : GetManageOfferingUrl(app.StoreDataId, offering.Id),
            AddPlanUrl = offering is null ? null : GetAddPlanUrl(app.StoreDataId, offering.Id),
            ActivePlanCount = offering?.Plans.Count(p => p.Status == PlanData.PlanStatus.Active) ?? 0,
            SupportMode = settings.SupportMode,
            AppName = app.Name,
            HeroTitle = settings.HeroTitle,
            HeroSubtitle = settings.HeroSubtitle,
            Description = settings.Description,
            PrimaryCallToAction = settings.PrimaryCallToAction,
            PrimaryCallToActionUrl = settings.PrimaryCallToActionUrl,
            DefaultCurrency = settings.DefaultCurrency,
            SuggestedAmounts = string.Join(", ", settings.SuggestedAmounts.Select(a => a.ToString("0.##", CultureInfo.InvariantCulture))),
            ProjectUrl = settings.Links.FirstOrDefault(link => string.Equals(link.Label, "Project", StringComparison.OrdinalIgnoreCase))?.Url,
            GitHubUrl = settings.Links.FirstOrDefault(link => string.Equals(link.Label, "GitHub", StringComparison.OrdinalIgnoreCase))?.Url,
            Visibility = settings.Visibility
        };
    }

    private OpenPatronPublicViewModel ToPublicViewModel(AppData app, OpenPatronAppSettings settings, OfferingData? offering)
    {
        return new OpenPatronPublicViewModel
        {
            AppId = app.Id,
            AppName = app.Name,
            OfferingId = offering?.Id,
            PublicPageUrl = GetPublicPageUrl(app.Id),
            SupportsOneTime = AllowsOneTime(settings),
            SupportsSubscriptions = AllowsSubscriptions(settings),
            HeroTitle = string.IsNullOrWhiteSpace(settings.HeroTitle) ? app.Name : settings.HeroTitle,
            HeroSubtitle = settings.HeroSubtitle,
            Description = settings.Description,
            PrimaryCallToAction = string.IsNullOrWhiteSpace(settings.PrimaryCallToAction)
                ? "Sponsor this project"
                : settings.PrimaryCallToAction,
            PrimaryCallToActionUrl = settings.PrimaryCallToActionUrl,
            DefaultCurrency = settings.DefaultCurrency,
            SuggestedAmounts = settings.SuggestedAmounts,
            Links = settings.Links
                .Where(link => !string.IsNullOrWhiteSpace(link.Url))
                .Select(link => new OpenPatronPublicLinkViewModel
                {
                    Label = link.Label,
                    Url = link.Url
                })
                .ToList(),
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
                    SubscribeUrl = Url.Action(nameof(Subscribe), new { appId = app.Id, planId = plan.Id, area = OpenPatronPlugin.Area }) ?? $"/apps/{app.Id}/openpatron/plans/{plan.Id}/subscribe",
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
        {
            return existing?.Id;
        }

        var offering = new OfferingData()
        {
            AppId = app.Id
        };
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

    private static bool AllowsOneTime(OpenPatronAppSettings settings)
        => settings.SupportMode is OpenPatronSupportMode.OneTimeOnly or OpenPatronSupportMode.Both;

    private static bool AllowsSubscriptions(OpenPatronAppSettings settings)
        => settings.SupportMode is OpenPatronSupportMode.SubscriptionOnly or OpenPatronSupportMode.Both;

    private static bool IsPublished(OpenPatronAppSettings settings)
        => settings.Visibility == OpenPatronVisibility.Published;

    private void ValidateSuggestedAmounts(string? suggestedAmounts)
    {
        if (string.IsNullOrWhiteSpace(suggestedAmounts))
        {
            return;
        }

        foreach (var token in suggestedAmounts.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            {
                ModelState.AddModelError(nameof(UpdateOpenPatronViewModel.SuggestedAmounts), $"'{token}' is not a valid positive amount.");
                break;
            }
        }
    }

    private static System.Collections.Generic.List<decimal> ParseSuggestedAmounts(string? suggestedAmounts)
    {
        if (string.IsNullOrWhiteSpace(suggestedAmounts))
        {
            return [];
        }

        return suggestedAmounts
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture))
            .Distinct()
            .OrderBy(value => value)
            .ToList();
    }

    private static string? NormalizeUrl(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static OpenPatronLink? CreateLink(string label, string? url)
        => string.IsNullOrWhiteSpace(url)
            ? null
            : new OpenPatronLink
            {
                Label = label,
                Url = url.Trim()
            };
}
