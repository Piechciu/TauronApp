## Context

`TauronApp` is a Blazor WebAssembly app whose `tariff-calculator` engine is complete and tested. `TariffCalculator.CalculateAll(year, totalEnergyKwh, zoneSharesPerTariff)` returns a `CostBreakdown` for every Group G tariff, where `zoneSharesPerTariff` is `IReadOnlyDictionary<TariffId, IReadOnlyDictionary<TariffZone, decimal>>` and each tariff's zone shares must sum to exactly 100% (with at most one decimal place per share). The web app currently has only a placeholder `Home.razor`. This change builds the screen that drives the engine.

Key engine facts that constrain the UI:
- Zones per tariff: G11 = 1 (`AllDay`), G12 = 2 (`Day`, `Night`), G12w = 2 (`Peak`, `OffPeak`), G13 = 3 (`MorningPeak`, `EveningPeak`, `Remaining`), G13s = 12 (season × day-type × period).
- `ValidateZoneShares` throws `ArgumentException` if a tariff's shares don't sum to exactly 100% or exceed one decimal place; an omitted zone is treated as 0%.
- The tariff/zone enums and `CostBreakdown` are the only types the UI needs from `TauronApp.Calculator`.

## Goals / Non-Goals

**Goals:**
- A single home screen where the user enters billing year and total annual energy, plus the zone-share split for every zone of every tariff.
- One card per tariff showing its total annual cost; click to expand and reveal the per-component breakdown and that tariff's zone-share inputs.
- A column chart at the bottom comparing total annual cost across all tariffs.
- Live recalculation as inputs change, with clear per-tariff validation feedback.
- Use Radzen.Blazor for inputs, cards/expansion, and the chart.

**Non-Goals:**
- No changes to the `tariff-calculator` engine or its rate catalog.
- No persistence, accounts, or saving/loading of input sets.
- No CSV/PDF export or sharing.
- No automatic derivation of zone shares from a simplified profile — the user supplies each zone share explicitly (per the chosen input model).

## Decisions

### Decision: User supplies the zone share for every zone, per tariff
Each tariff card's expanded view contains a numeric input for each of that tariff's zones. The user enters the percentage for every zone directly (G13s shows all 12). This is accurate and matches the engine's input model 1:1, with no hidden inference.
- **Alternative considered**: a shared "usage profile" the app maps onto each tariff's zones. Rejected — it hides assumptions, is inaccurate for G13/G13s, and the user explicitly asked to provide each zone share.
- **Consequence**: G13s is verbose; mitigated by grouping its 12 zones by season/day-type and by seeding sensible defaults so results appear immediately.

### Decision: Seed every tariff with valid default shares
On first load each tariff starts with a default share set that sums to 100% (e.g. G11 = 100% AllDay; G12 = 60/40 Day/Night; G13s defaults split across its 12 zones). This guarantees an immediate, valid calculation and gives the user a starting point to adjust rather than a blank form.

### Decision: View-model holds editable shares; map to engine types on calculate
The page keeps a mutable view-model (`year`, `totalEnergyKwh`, and a `Dictionary<TariffId, Dictionary<TariffZone, decimal>>`). On any input change it validates each tariff's shares (sum = 100%, ≤ 1 decimal) and, for tariffs that pass, calls the engine. A tariff that fails validation shows an inline message instead of a cost; valid tariffs still calculate. The default zone list per tariff is derived once from the engine's catalog so the UI never hard-codes which zones a tariff has.
- **Alternative considered**: call `CalculateAll` only when every tariff is valid. Rejected — one bad tariff shouldn't blank the whole screen; per-tariff `Calculate` lets valid cards keep showing results.

### Decision: Radzen.Blazor for components
Use `RadzenNumeric` for year/energy/share inputs, `RadzenCard` with a click-toggled expansion region for the tariff cards, and `RadzenChart`/`RadzenColumnSeries` for the comparison chart. Register Radzen services in `Program.cs` and add its theme CSS + JS to `wwwroot/index.html`. The package is added centrally via `Directory.Packages.props`.
- **Alternative considered**: hand-rolled CSS cards + a JS charting lib. Rejected — the user asked for Radzen, and it covers cards, inputs, and charts in one dependency.

### Decision: Cards sorted cheapest-first
Cards (and optionally the chart order) are sorted by total ascending so the best-value tariff is immediately visible, with the cheapest visually highlighted.

## Risks / Trade-offs

- **G13s twelve-field input is tedious** → group the inputs by season/day-type, seed valid defaults, and show a running per-tariff total so the user can reach 100% easily.
- **Shares not summing to 100% block that tariff** → live inline validation with the current sum shown; other tariffs continue to calculate so the screen is never fully blank.
- **`CalculateAll` throws if any tariff is invalid** → call per-tariff `Calculate` for validated tariffs instead of `CalculateAll`, isolating failures.
- **Radzen CSS/JS not wired correctly in WASM** → verify the theme stylesheet and `Radzen.Blazor.js` references in `index.html` and that `dotnet build` + a manual page load render components before considering the task done.
- **Floating-point share sums** → the engine requires an exact 100% with ≤ 1 decimal; the UI restricts inputs to one decimal place and compares the sum at that precision to match the engine's rule.

## Open Questions

- Currency/number formatting locale (PLN, Polish formatting) — assume `pl-PL` / "zł" unless told otherwise.
- Whether to also show the chart broken down by component (stacked) later — out of scope for now (totals only).
