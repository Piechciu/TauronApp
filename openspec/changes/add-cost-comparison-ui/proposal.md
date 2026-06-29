## Why

The `tariff-calculator` engine can already compute and compare the annual cost of every Group G tariff, but the app has no way to drive it — the home page is the default "Hello, world!" placeholder. Users need a single screen where they enter their consumption details and immediately see, and visually compare, what each TAURON tariff would cost them.

## What Changes

- Replace the placeholder home page with an interactive cost-comparison screen.
- Add an input area where the user provides the values needed to calculate costs: billing year, total annual energy usage (kWh), and the time-of-use split that drives per-tariff zone shares.
- Render one card per tariff (G11, G12, G12w, G13, G13s), each showing the tariff name and its overall annual cost; cards are sorted so the cheapest is easy to spot.
- Make each card expandable on click to reveal the per-component cost breakdown (energy, distribution, surcharges, fixed fees, capacity fee) returned by the calculator.
- Add a column chart at the bottom comparing the total annual cost across all tariffs.
- Recalculate results live as inputs change, with validation feedback when inputs are incomplete or invalid.
- Introduce the **Radzen.Blazor** component library for the cards, expand/collapse, numeric inputs, and chart, and wire the calculator into the Blazor WebAssembly app via dependency injection.

## Capabilities

### New Capabilities
- `cost-comparison-ui`: The interactive home-page screen that collects consumption inputs, calculates all Group G tariffs, presents each as an expandable cost card, and visualizes the comparison as a column chart.

### Modified Capabilities
<!-- None. The tariff-calculator engine and its requirements are unchanged; this change only consumes its existing public API. -->

## Impact

- **New/changed code**: `src/TauronApp/Pages/Home.razor` (rewritten), new input/card/chart components under `src/TauronApp/`, mapping from UI inputs to the calculator's per-tariff `zoneSharesPerTariff` model.
- **DI / startup**: `src/TauronApp/Program.cs` registers `TariffCalculator` and Radzen services.
- **Dependencies**: adds the `Radzen.Blazor` NuGet package (and its CSS/JS includes in `wwwroot/index.html`); managed centrally via `src/Directory.Packages.props`.
- **Consumes existing API**: `TariffCalculator.CalculateAll(...)` returning `CostBreakdown` per `TariffId`; no changes to `TauronApp.Calculator`.
- **No backend/persistence changes**: all calculation stays client-side in WebAssembly.
