## 1. Add Radzen and wire up the app

- [x] 1.1 Add the `Radzen.Blazor` package version to `src/Directory.Packages.props` and a `PackageReference` in `src/TauronApp/TauronApp.csproj`
- [x] 1.2 Register Radzen services and the `TariffCalculator` in `src/TauronApp/Program.cs` (DI)
- [x] 1.3 Add the Radzen theme CSS and `Radzen.Blazor.js` references to `src/TauronApp/wwwroot/index.html`
- [x] 1.4 Add Radzen `@using` entries to `src/TauronApp/_Imports.razor`; add `<RadzenComponents />` (dialog/notification host) to `MainLayout.razor`
- [x] 1.5 Verify the app builds (`dotnet build src/TauronApp.slnx`) and Radzen renders a sample component on the home page

## 2. View-model and engine mapping

- [x] 2.1 Create a page view-model holding `Year`, `TotalEnergyKwh`, and a mutable `Dictionary<TariffId, Dictionary<TariffZone, decimal>>` of zone shares
- [x] 2.2 Derive the zone list per tariff from the engine catalog (no hard-coded zone membership) and seed valid default shares per tariff (each summing to 100%)
- [x] 2.3 Implement per-tariff validation: shares sum to exactly 100% with at most one decimal place; total energy is a valid non-negative number
- [x] 2.4 Implement calculation: for each tariff with valid inputs, call `TariffCalculator.Calculate` and collect `CostBreakdown`; invalid tariffs produce a validation message instead of a cost
- [x] 2.5 Recompute results whenever year, total energy, or any zone share changes (live update)

## 3. Global input UI

- [x] 3.1 Build the global inputs area on `Home.razor` with `RadzenNumeric` for year and total annual energy (kWh)
- [x] 3.2 Show validation feedback for missing/invalid global inputs

## 4. Tariff cards

- [x] 4.1 Render one `RadzenCard` per tariff showing tariff name and overall annual cost (formatted as PLN)
- [x] 4.2 Sort cards by total annual cost ascending and visually distinguish the cheapest
- [x] 4.3 Make each card collapsed by default and expandable on click (toggle state per card)
- [x] 4.4 In the expanded view, show the per-component breakdown (energy, distribution, surcharges, fixed fees, capacity fee) and the zone-share inputs for that tariff
- [x] 4.5 For G13s, group its twelve zone inputs by season/day-type and show a running per-tariff share total with inline validation

## 5. Comparison chart

- [x] 5.1 Add a `RadzenChart` with a `RadzenColumnSeries` below the cards, one column per tariff (labeled by tariff name) bound to total annual cost
- [x] 5.2 Ensure the chart updates live when inputs change

## 6. Polish and verification

- [x] 6.1 Apply PLN/`pl-PL` formatting for costs and confirm breakdown components sum to the displayed total
- [x] 6.2 Run `dotnet format src/TauronApp.slnx` and `dotnet build src/TauronApp.slnx`
- [ ] 6.3 Manually run the app and verify: defaults calculate on load, cards expand/collapse, invalid shares block only their own tariff, and the chart reflects all tariffs and updates live _(blocked in this environment: the Blazor dev server needs the ASP.NET Core 10 shared runtime, which is not installed; verify locally with `dotnet run --project src/TauronApp`)_
