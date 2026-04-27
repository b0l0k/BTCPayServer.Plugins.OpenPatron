# BTCPayServer.Plugins.OpenPatron

OpenPatron is a BTCPay Server plugin focused on sponsor pages for open source maintainers.

## Repository layout

- [BTCPayServer.Plugins.OpenPatron](BTCPayServer.Plugins.OpenPatron) - main plugin project
- [BTCPayServer.Plugins.OpenPatron.Tests](BTCPayServer.Plugins.OpenPatron.Tests) - fast test project scaffold
- [ConfigBuilder](ConfigBuilder) - helper used for local debug plugin wiring
- [Docs](Docs) - repository documentation
- [submodules/btcpayserver](submodules/btcpayserver) - expected BTCPay Server source submodule

## Current scope

- Registers a new BTCPay app type: `OpenPatron`
- Adds store navigation for creating and managing OpenPatron apps
- Provides an authenticated settings page for each OpenPatron app
- Provides a public sponsor page at `/apps/{appId}/openpatron`
- Stores page branding data in `AppData.Settings`
- Links each OpenPatron app to a Subscriptions `Offering`
- Uses BTCPay Subscriptions as the recurring sponsorship engine

## Development notes

- The repository is scaffolded to mirror the USDt plugin repository shape
- Git submodule metadata is declared in [.gitmodules](.gitmodules)
- The BTCPayServer submodule is initialized under [submodules/btcpayserver](submodules/btcpayserver)
- CI workflows are available under [.github/workflows](.github/workflows)
- Recurring checkout flows through the Subscriptions plan checkout UI
