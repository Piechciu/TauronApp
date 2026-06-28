## Context

TauronApp is a .NET 10 Blazor WebAssembly app with an existing xUnit test project (`src/TauronApp.Tests`). There is no calculation logic yet. The 2026 Group G tariff rates are documented in `tarrifs/tauron_taryfy_2026.md` (all gross / brutto). We need a UI-independent engine that, given how much energy a customer uses and how that usage is spread across each tariff's time zones, returns the annual cost per tariff so plans can be compared.

Each tariff has a different set of time zones:
- **G11**: one zone (all-day).
- **G12 / G12w**: two zones (day/night, peak/off-peak).
- **G13**: three zones (morning peak, evening peak, remaining).
- **G13s**: distribution-only (no TAURON Sprzedaż energy price), modeled as **12 explicit zones** — the full cross product of season (summer/winter) × day type (workday/holiday) × period (day peak / day off-peak / night).

## Goals / Non-Goals

**Goals:**
- A `TariffCalculator` that computes annual cost for any Group G tariff from total energy + per-zone percentage split.
- Return an explainable per-component cost breakdown, not just a single number.
- Tariff rates stored in a static JSON file (deserialized at runtime) so they are easy to audit against the source doc and to add a new year's rates without code changes.
- Engine lives in its own class library with no Blazor/UI dependency, fully unit-tested.

**Non-Goals:**
- No UI / Blazor pages in this change (consumed by a later change).
- No hour-by-hour load profiling — we accept a percentage split per zone as input.
- No meter-phase or billing-cycle choice for now: a three-phase meter and monthly billing are assumed. The JSON still carries the full rate tables so these can become inputs later without a data change.
- No persistence, no localization, no currency formatting (return raw decimals in PLN).
- No automatic recommendation of the "best" tariff (UI concern; the breakdown enables it later).

## Decisions

### 1. New class library `TauronApp.Calculator`
Keep the math separate from the Blazor app so it is testable and reusable. `TauronApp` references it, and a new dedicated `TauronApp.Calculator.Tests` project tests it directly (mirroring the existing `TauronApp` / `TauronApp.Tests` pairing). Target `net10.0`, `Nullable` + `ImplicitUsings` enabled to match existing projects.

*Alternative considered:* put logic directly in the Blazor project. Rejected — couples domain logic to UI and complicates testing.

### 2. Use `decimal` for all money and rates
Currency math; avoid binary floating-point rounding error. Rates from the doc are exact decimals.

### 3. Strongly-typed tariff and zone identifiers (no magic strings)
Every tariff and every tariff zone is a value in a C# enum defined in code; nothing in the engine's public surface is keyed by a raw string.
- `TariffId` enum: `G11`, `G12`, `G12w`, `G13`, `G13s`.
- `TariffZone` enum: one value per distinct zone across all tariffs — `AllDay` (G11); `Day`, `Night` (G12); `Peak`, `OffPeak` (G12w); `MorningPeak`, `EveningPeak`, `Remaining` (G13); and the 12 G13s zones named by season/day-type/period (e.g. `SummerWorkdayDayPeak`, `SummerWorkdayDayOffPeak`, `SummerWorkdayNight`, … `WinterHolidayNight`).

A `TariffDefinition` lists the `TariffZone` values that belong to it (in order); a zone carries its energy price and distribution variable fee. `CalculationInput.ZoneShares` is a `Dictionary<TariffZone, decimal>`, not a string map, so callers get compile-time checking. The JSON refers to tariffs and zones by enum *name*, and `System.Text.Json` (with `JsonStringEnumConverter`) deserializes those names to the enums — if the JSON names a zone that is not in the enum, deserialization fails loudly rather than silently introducing a stray string.

*Alternative considered:* per-tariff zone enums instead of one flat `TariffZone`. Rejected for now — a single flat enum keeps `ZoneShares` one type and the calculation loop uniform; `TariffDefinition` already scopes which zones are valid for a tariff.

### 4. Tariffs modeled as data, stored in JSON per year
Rates live in a static JSON file embedded in the calculator library (e.g. `Rates/tauron-rates-2026.json`, marked `EmbeddedResource`), deserialized into the domain model at runtime. The JSON for a year contains:
- `year` — the year the rates apply to.
- `tariffs` — each keyed by `TariffId` and holding an ordered set of zones; each zone is keyed by `TariffZone` and has an energy price (PLN/kWh, zero for G13s) and a distribution variable fee (PLN/kWh).
- `commonFees` — fixed network fee per meter phase, subscription fee per billing cycle, the per-kWh quality / OZE / cogeneration rates, and the capacity-fee brackets (identical across tariffs).

A `RateCatalog` type is the deserialized shape; a `RateCatalogProvider` loads the embedded JSON for a requested year and exposes the `TariffDefinition`s and `CommonFees` to the calculator. The provider keys catalogs by year so multiple years can coexist; adding a future year is just dropping in a new JSON file (and embedding it). Deserialization uses `System.Text.Json` with enum names mapped to `TariffId` and `TariffZone`.

*Alternatives considered:* (a) hard-coded C# constants — rejected; updating a year means a code change and recompile, and it is harder to diff against the source table. (b) loading JSON from `wwwroot` over HTTP — rejected; the engine must stay UI/host-independent, so rates are embedded in the library instead.

### 5. Input model — total energy + percentage split per zone
`CalculationInput` carries `Year` (selects which rate catalog to use), `TotalEnergyKwh` (interpreted as the annual consumption), and `ZoneShares`: a `Dictionary<TariffZone, decimal>` mapping zone → percentage for the tariff being calculated. For now a three-phase meter and monthly billing are assumed, so the fixed network fee and subscription fee are taken from the three-phase and monthly entries of the catalog rather than from input. Because each tariff has its own zones, percentages are supplied per tariff. `CalculateAll` accepts a per-tariff dictionary of zone shares plus the shared year and energy values.

Percentages must sum to ~100% (small tolerance for rounding); otherwise the input is rejected. A single-zone tariff (G11) accepts 100% implicitly.

### 6. Cost formula (annual)
```
energyCost        = Σ_zone (TotalEnergyKwh × share_zone × energyPrice_zone)
distributionCost  = Σ_zone (TotalEnergyKwh × share_zone × distVariable_zone)
surcharges        = TotalEnergyKwh × (quality + oze + cogeneration)
fixedCost         = 12 × (fixedNetworkFee[three-phase] + subscriptionFee[monthly])
capacityFee       = 12 × capacityBracket(TotalEnergyKwh)
total             = energyCost + distributionCost + surcharges + fixedCost + capacityFee
```
`CostBreakdown` returns each line plus the total and the tariff id, so the UI can show a table.

### 7. Capacity fee bracket from total annual energy
The capacity fee is selected by the annual consumption bracket (`<500`, `500–1200`, `1200–2800`, `>2800` kWh). Since `TotalEnergyKwh` is the annual figure, the bracket is derived directly from it.

## Risks / Trade-offs

- **G13s 12-zone model** → its rates depend on season *and* day type, so it is modeled as the full cross product: season (summer/winter) × day type (workday/holiday) × period (day peak / day off-peak / night) = 12 zones. This makes its zone-share input larger than other tariffs (12 percentages that must sum to 100%). Mitigation: the 12 zones are defined explicitly as `TariffZone` enum values with descriptive names; the input map is keyed by those enum values, so the calculation mechanism stays uniform across all tariffs.
- **Percentage input vs. real usage** → a percentage split is an approximation of true hourly load. Accepted as a deliberate simplification (a Non-Goal to do hour-by-hour); the breakdown makes assumptions visible.
- **Rate drift** → rates change yearly and the quality fee note says it may change. Mitigation: rates isolated in a per-year JSON file (`Rates/tauron-rates-<year>.json`) loaded by year, so a future year is a new data file, not a code change.
- **Rounding** → using `decimal` throughout; no rounding applied inside the engine (callers/UI round for display) to keep results composable and testable.
