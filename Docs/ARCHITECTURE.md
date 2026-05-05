# OpenPatron — Architecture

## Overview

OpenPatron is a BTCPay Server plugin that provides a block-based sponsor page builder. Store owners create "OpenPatron" apps, configure a page layout with themed blocks, and publish a public page where visitors can make one-time contributions or subscribe to recurring plans.

## Project Structure

```
BTCPayServer.Plugins.OpenPatron/
├── OpenPatronPlugin.cs            # Plugin entry point, DI registration
├── OpenPatronAppType.cs           # App type definition (routing, defaults)
├── OpenPatronOfferingResolver.cs  # Subscription offering selection logic
├── Controllers/
│   └── UIOpenPatronController.cs  # All HTTP endpoints (admin + public)
├── Models/
│   ├── OpenPatronAppSettings.cs   # Persisted settings (sections, blocks, theme)
│   └── BlockSettings.cs           # Per-block-type settings classes
├── ViewModels/
│   ├── UpdateOpenPatronViewModel.cs      # Admin settings form model
│   ├── OpenPatronPublicViewModel.cs      # Public page view model
│   └── BlockRenderContext.cs             # Context passed to block partials
├── Services/
│   ├── BlockRegistry.cs           # Block type metadata, layout presets
│   ├── BlockSettingsHelper.cs     # JSON extraction, Gravatar, Markdown
│   ├── SponsorWallService.cs      # Recent settled invoice queries
│   ├── FundingProgressService.cs  # Total raised calculation
│   └── GitHubRepoService.cs       # GitHub API client (stars, language)
├── Views/UIOpenPatron/
│   ├── Update.cshtml              # Admin block editor UI
│   ├── PublicPage.cshtml          # Public sponsor page
│   ├── NavExtension.cshtml        # Header nav menu item
│   └── Blocks/                    # 9 block partial views
├── BTCPayServer.Plugins.OpenPatron.Tests/
│   ├── FastTests.cs               # Unit tests
│   └── PlaywrightTests.cs         # End-to-end browser tests
├── ConfigBuilder/
│   └── Program.cs                 # Generates appsettings.dev.json for local dev
└── submodules/
    └── btcpayserver/              # Git submodule (BTCPay Server source)
```

## Plugin Bootstrap

`OpenPatronPlugin` extends `BaseBTCPayServerPlugin` and registers all services in its `Execute()` method:

| Registration | Type | Purpose |
|---|---|---|
| `AppBaseType` → `OpenPatronAppType` | Singleton | Registers the "OpenPatron" app type with BTCPay |
| `GitHubRepoService` | Transient | Fetches repo metadata from GitHub API |
| `SponsorWallService` | Transient | Queries recent settled invoices for sponsor wall |
| `FundingProgressService` | Transient | Sums settled invoices for funding progress bar |
| Named `HttpClient` ("GitHub") | — | HTTP client for GitHub API calls |
| UI Extension `header-nav` | — | Adds "Open Patron" to BTCPay's nav menu |

## Storage

### Where Data Lives

OpenPatron stores **all settings** inside BTCPay Server's `Apps` table using the built-in `AppData` mechanism. There is no custom database schema.

```
┌─────────────────────────────────────────────┐
│  Apps table (BTCPay Server DB)              │
├──────────┬──────────────────────────────────┤
│  Id      │  unique app ID                   │
│  AppType │  "OpenPatron"                    │
│  StoreId │  owning store                    │
│  Name    │  user-given app name             │
│  Settings│  JSON blob (OpenPatronAppSettings)│
└──────────┴──────────────────────────────────┘
```

The `Settings` column holds a JSON-serialized `OpenPatronAppSettings` object containing:

- **Page metadata** — `PageType` (Personal / Project), `PageTypeConfirmed`, `Visibility`
- **Layout** — `PageLayoutPreset` (e.g. `"8-4"`), `Sections` (list of columns with blocks)
- **Theme** — `PageTheme` (accent color, border radius, spacing, shadow, typography, background)
- **Global settings** — `DefaultCurrency`, `OfferingId` (link to Subscriptions offering)

### Settings Serialization

```
OpenPatronAppSettings
├── PageType: Project
├── PageTypeConfirmed: true
├── PageLayoutPreset: "8-4"
├── Sections[]
│   ├── Section { Id: "col-1", Width: 8, Blocks: [...] }
│   └── Section { Id: "col-2", Width: 4, Blocks: [...] }
├── Theme
│   ├── AccentColor: "#51b13e"
│   ├── SecondaryColor: "#CEDC21"
│   ├── BorderRadius: "8px"
│   ├── BlockSpacing: "1rem"
│   ├── ShadowStyle: "subtle"
│   ├── TypographyStyle: "standard"
│   └── BackgroundStyle: "flat"
├── DefaultCurrency: "USD"
├── Visibility: Published
└── OfferingId: "abc123..."
```

### Read/Write Flow

```
Write:  Controller → OpenPatronAppSettings → app.SetSettings(settings) → appService.UpdateOrCreateApp(app) → DB
Read:   DB → appService.GetApp(appId) → app.GetSettings<OpenPatronAppSettings>() → Controller
```

### Related Data (Not Owned by OpenPatron)

| Data | Source | How It's Queried |
|---|---|---|
| Invoices (one-time contributions) | BTCPay `Invoices` table | By app search term via `InvoiceSearchData` |
| Offerings & Plans (subscriptions) | BTCPay Subscriptions plugin | By `OfferingId` stored in settings |

## Block System

Pages are built from blocks organized into sections (columns). Each block has a type, settings, and an optional theme override.

### Block Types

| Type Key | Settings Class | Purpose |
|---|---|---|
| `profile-hero` | `ProfileHeroSettings` | Avatar, name, bio, social links (personal pages) |
| `project-hero` | `ProjectHeroSettings` | Headline, subtitle, maintainer card (project pages) |
| `funding-progress` | `FundingProgressSettings` | Progress bar toward a funding goal |
| `description` | `DescriptionSettings` | Markdown heading + content |
| `projects-grid` | `ProjectsGridSettings` | Grid of project cards with GitHub stats |
| `subscription-tiers` | `SubscriptionTiersSettings` | Grid of subscription plan cards |
| `quick-support` | `QuickSupportSettings` | Suggested one-time amount buttons |
| `sponsor-wall` | `SponsorWallSettings` | Recent contributions list |
| `one-time-payment` | `OneTimePaymentSettings` | Compact sidebar panel with amount input |

### Data Model

```
PageSection
├── Id: "col-1"
├── Width: 8          (Bootstrap column width)
└── Blocks[]
    └── BlockDefinition
        ├── Id: "a1b2c3d4e5f6"   (auto-generated 12-char hex)
        ├── Type: "profile-hero"
        ├── Settings: { ... }     (JObject, block-type-specific)
        └── Theme?: { AccentColor?, BorderRadius? }
```

### Layout Presets

| Preset | Columns |
|---|---|
| `"8-4"` | 8-wide main + 4-wide sidebar |
| `"4-8"` | 4-wide sidebar + 8-wide main |
| `"6-6"` | Two equal columns |
| `"12"` | Single full-width column |

### Block Registry

`BlockRegistry` (static service) maps each block type to its metadata:

- Display name and description
- Razor partial view path (`Views/UIOpenPatron/Blocks/_Block_{Type}.cshtml`)
- Settings class `Type` (for JSON schema generation and deserialization)

## Controller Endpoints

`UIOpenPatronController` handles all routes under `apps/`.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | `{appId}/settings/openpatron` | Store viewer | Admin settings page |
| POST | `{appId}/settings/openpatron` | Store modifier | Save settings |
| GET | `{appId}/openpatron` | Anonymous | Public sponsor page |
| POST | `{appId}/openpatron/contribute` | Anonymous | Create one-time invoice |
| POST | `{appId}/openpatron/plans/{planId}/subscribe` | Anonymous | Create subscription checkout |
| GET | `openpatron/schema.json` | Anonymous | JSON Schema for blocks (cached 24h) |
| GET | `{appId}/openpatron/badge.svg` | Anonymous | Embeddable SVG badge (cached 5m) |

## Runtime Data Enrichment

Settings stored in the database are static. At render time, the controller enriches the public view model with live data:

```
OpenPatronPublicViewModel
├── Sections, Theme         ← from stored settings
├── AmountRaised            ← FundingProgressService (sum of settled invoices)
├── FundingPercentage       ← computed from AmountRaised / goal
├── SponsorWallEntries[]    ← SponsorWallService (recent settled invoices)
├── Plans[]                 ← Offerings table (via OfferingId)
└── GitHub stars/language   ← GitHubRepoService (cached 1 hour)
```

## Payment Flows

### One-Time Contribution

1. Visitor enters amount on public page
2. `POST /apps/{appId}/openpatron/contribute` with amount
3. Controller creates an invoice via `UIInvoiceController.CreateInvoiceCoreRaw()`
   - Tags the invoice with the app search term (enables sponsor wall and funding queries)
4. Visitor is redirected to BTCPay invoice checkout
5. Once settled, the invoice appears in sponsor wall and funding totals

### Subscription

1. Visitor clicks "Subscribe" on a plan card
2. `POST /apps/{appId}/openpatron/plans/{planId}/subscribe`
3. Controller redirects to BTCPay Subscriptions plugin's `UIPlanCheckout`
4. Subscriptions plugin handles the recurring billing lifecycle

## Offering Resolution

`OpenPatronOfferingResolver.SelectPreferredOffering()` picks the best offering for display:

1. If no offerings exist → `null`
2. Score each offering by plan count + feature count + recency
3. If a preferred `OfferingId` is set in settings and its offering has plans/features → use it
4. Otherwise fall back to the highest-scored offering

## View Rendering

```
PublicPage.cshtml
├── Applies PageTheme as CSS custom properties (--op-accent, --op-secondary, etc.)
├── Iterates Sections (Bootstrap row > col-md-{Width})
│   └── Iterates Blocks in each section
│       └── Renders _Block_{Type}.cshtml partial
│           └── Receives BlockRenderContext { Block, Page }
│               └── Uses BlockSettingsHelper to extract typed settings from JObject
└── Footer: "Powered by BTCPay Server and Open Patron"
```

## External Integrations

### GitHub API

`GitHubRepoService` fetches repository metadata for the `projects-grid` block:

- Endpoint: `GET https://api.github.com/repos/{owner}/{repo}`
- User-Agent: `BTCPayServer-OpenPatron/1.0`
- Caching: 1 hour via `IMemoryCache`
- Parsed URL format: `https://github.com/{owner}/{repo}`

### Gravatar

`BlockSettingsHelper.ComputeGravatarUrl()` generates avatar URLs from email addresses using MD5 hashing. Falls back to GitHub avatar if no email is provided.

## Testing

| Suite | Runner | Scope |
|---|---|---|
| `FastTests.cs` | xUnit | Unit tests for settings defaults, serialization, block registry, migrations |
| `PlaywrightTests.cs` | Playwright + xUnit | E2E tests for template picker, block editor, theme persistence, public page rendering |

## Local Development

The `ConfigBuilder` project generates `appsettings.dev.json` in the BTCPay Server submodule directory, pointing `DEBUG_PLUGINS` to the compiled plugin DLL. This lets BTCPay Server load the plugin during development without packaging.
