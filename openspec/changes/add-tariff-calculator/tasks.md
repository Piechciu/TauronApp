## 1. Project setup

- [x] 1.1 Create `src/TauronApp.Calculator/TauronApp.Calculator.csproj` as a `net10.0` class library with `Nullable` and `ImplicitUsings` enabled
- [x] 1.2 Add the calculator project to `src/TauronApp.slnx` and add a `ProjectReference` from `src/TauronApp/TauronApp.csproj`
- [x] 1.3 Create `src/TauronApp.Calculator.Tests/TauronApp.Calculator.Tests.csproj` (xUnit.v3, `net10.0`, mirroring `TauronApp.Tests` package setup), with a `ProjectReference` to the calculator project, and add it to `src/TauronApp.slnx`
- [x] 1.4 Confirm `dotnet build` succeeds for the whole solution

## 2. Domain model

- [x] 2.1 Add a `TariffId` enum (G11, G12, G12w, G13, G13s) and a `TariffZone` enum with one value per distinct zone (AllDay; Day/Night; Peak/OffPeak; MorningPeak/EveningPeak/Remaining; the 12 G13s season/day-type/period zones) — no string keys
- [x] 2.2 Add a `TariffZoneRate` model (zone `TariffZone`, energy price PLN/kWh, distribution variable fee PLN/kWh) using `decimal`, and a `TariffDefinition` model (`TariffId` + ordered `TariffZoneRate`s)
- [x] 2.3 Add `CalculationInput` (year, total annual energy kWh, `Dictionary<TariffZone, decimal>` zone shares) and `CostBreakdown` (`TariffId`, energy, distribution, surcharges, fixed fees, capacity fee, total)

## 3. Rate catalog (JSON, per year)

- [x] 3.1 Define the JSON-serializable shape (`RateCatalog` with `year`, `tariffs`, `commonFees`) and `System.Text.Json` options with `JsonStringEnumConverter` mapping names to `TariffId` and `TariffZone` (unknown names fail deserialization)
- [x] 3.2 Author `Rates/tauron-rates-2026.json` with G11, G12, G12w, G13 zones/rates from `tarrifs/tauron_taryfy_2026.md`, and mark it as an `EmbeddedResource` in the csproj
- [x] 3.3 Add the G13s entry with 12 zones (summer/winter × workday/holiday × day peak / day off-peak / night), energy price 0, and each zone's distribution variable fee
- [x] 3.4 Add the `commonFees` section: fixed network fee per phase, subscription fee per billing cycle, quality/OZE/cogeneration per-kWh rates, and capacity fee brackets
- [x] 3.5 Implement `RateCatalogProvider` that loads and deserializes the embedded JSON for a requested year, caches it, and reports an error for years with no catalog

## 4. TariffCalculator

- [x] 4.1 Implement zone-share validation (percentages sum to ~100% within tolerance; omitted zones default to 0%; reject otherwise)
- [x] 4.2 Implement energy and distribution cost as the sum over zones of `total × share × rate`
- [x] 4.3 Implement per-kWh surcharges over total energy (quality + OZE + cogeneration)
- [x] 4.4 Implement fixed fees: `12 × (fixedNetwork[three-phase] + subscription[monthly])`
- [x] 4.5 Implement capacity fee bracket selection from total annual energy, applied `× 12`
- [x] 4.6 Resolve the rate catalog for `input.Year` via `RateCatalogProvider` before computing
- [x] 4.7 Implement `Calculate(tariffId, input)` returning a `CostBreakdown` whose components sum to the total
- [x] 4.8 Implement `CalculateAll(...)` over all five tariffs given the year and total energy plus per-tariff zone shares

## 5. Tests

- [x] 5.0 In `src/TauronApp.Calculator.Tests`, add a `TariffCalculatorTests` class (xUnit.v3) covering the cases below
- [x] 5.1 Test G11 single-zone total against a hand-computed expected value
- [x] 5.2 Test G12 multi-zone split allocates energy by percentage and uses per-zone rates
- [x] 5.3 Test validation rejects zone shares that do not sum to ~100% and accepts within tolerance
- [x] 5.4 Test surcharges, fixed fees (three-phase + monthly), and capacity fee (per bracket) are applied correctly
- [x] 5.5 Test G13s has 12 zones, yields a zero energy-price component, and produces distribution-only energy cost
- [x] 5.6 Test the JSON catalog deserializes for 2026, that requesting an unavailable year is reported as an error, and that an unknown tariff/zone name in the JSON fails to deserialize
- [x] 5.7 Test `CalculateAll` returns a breakdown for every tariff and that components sum to the total
- [x] 5.8 Confirm `dotnet test` passes
