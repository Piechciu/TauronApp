## Why

TauronApp's whole purpose is to let a residential customer compare the cost of TAURON Group G tariff plans for their own consumption, but no calculation logic exists yet. We need a core, UI-independent calculation engine that turns a customer's energy usage into a cost per tariff so the comparison can be built on top of it.

## What Changes

- Add a new `TauronApp.Calculator` class library project (referenced by the Blazor app and the test project) holding all tariff math, free of any UI dependency.
- Introduce a `TariffCalculator` class that, given total annual energy usage and the percentage split of that usage across a tariff's time zones, returns the full annual cost for a tariff plan. For now it assumes a three-phase meter and monthly billing.
- Store the Group G tariff rates (G11, G12, G12w, G13, G13s) in a static, per-year JSON file that is deserialized at runtime: per-zone energy prices, per-zone distribution variable fees, and the common fees (fixed network, subscription, quality, OZE, cogeneration, capacity). The file is keyed by year so a new year's rates can be added without code changes.
- Model G13s as 12 explicit zones — the cross product of season (summer/winter) × day type (workday/holiday) × period (day peak / day off-peak / night).
- Identify every tariff and zone with strongly-typed enums (`TariffId`, `TariffZone`) defined in code — no magic strings in inputs, outputs, or the calculation; the JSON references these names and deserializes to the enums.
- Produce a cost breakdown (energy, distribution variable, fixed/subscription, per-kWh surcharges, capacity fee) per tariff, not just a single total, so results are explainable.
- Add a dedicated `TauronApp.Calculator.Tests` project (xUnit.v3) covering the calculator.

## Capabilities

### New Capabilities
- `tariff-calculator`: Compute the annual cost of each TAURON Group G tariff from a customer's total energy usage and the percentage split of that usage across the tariff's time zones, using year-specific rates loaded from a JSON catalog, and return a per-component cost breakdown.

### Modified Capabilities

(none)

## Impact

- New project `src/TauronApp.Calculator/` and a project reference from `src/TauronApp/TauronApp.csproj`.
- New test project `src/TauronApp.Calculator.Tests/` (referencing the calculator), added to `src/TauronApp.slnx`. The existing `TauronApp.Tests` is left untouched.
- New embedded JSON rate file (e.g. `src/TauronApp.Calculator/Rates/tauron-rates-2026.json`) sourced from `tarrifs/tauron_taryfy_2026.md` (2026 gross rates).
- No UI changes in this change; the engine is consumed by a later UI change.
