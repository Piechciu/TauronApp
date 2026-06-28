## ADDED Requirements

### Requirement: Identify tariffs and zones with typed identifiers

The system SHALL identify every tariff and every tariff zone with a strongly-typed enumerated identifier defined in code, and SHALL NOT key its public inputs or outputs by raw strings. Zone shares, tariff selection, and cost results SHALL reference these typed identifiers.

#### Scenario: Tariff and zone referenced by typed identifier

- **WHEN** a caller specifies a tariff and supplies zone shares
- **THEN** the tariff and each zone are referenced by their enumerated identifiers, giving compile-time checking, rather than by free-form strings

#### Scenario: Unknown identifier in rate data is rejected

- **WHEN** the loaded rate data names a tariff or zone that has no corresponding enumerated identifier
- **THEN** loading fails with an error rather than silently accepting an unrecognized identifier

### Requirement: Compute annual cost for a single tariff

The system SHALL compute the total annual cost for a chosen Group G tariff given the customer's total annual energy usage and the percentage split of that usage across the tariff's time zones.

#### Scenario: Single-zone tariff (G11)

- **WHEN** the caller requests a G11 calculation with a total annual energy usage and a 100% share in the all-day zone
- **THEN** the system returns a total annual cost computed from the G11 energy price, distribution variable fee, per-kWh surcharges, fixed fees, and capacity fee

#### Scenario: Multi-zone tariff (G12)

- **WHEN** the caller requests a G12 calculation with a total annual energy usage split as percentages between the day and night zones
- **THEN** energy assigned to each zone equals the total usage multiplied by that zone's percentage, and the total cost reflects each zone's own energy and distribution rates

### Requirement: Distribute energy across zones by percentage

The system SHALL allocate energy to each zone as `total energy × zone percentage`, and SHALL reject input whose zone percentages do not sum to approximately 100%.

#### Scenario: Percentages sum to 100

- **WHEN** zone percentages for a tariff sum to 100% (within a small rounding tolerance)
- **THEN** the calculation proceeds and each zone receives its proportional share of the total energy

#### Scenario: Percentages do not sum to 100

- **WHEN** zone percentages for a tariff sum to a value outside the accepted tolerance of 100%
- **THEN** the system rejects the input with a validation error rather than returning a cost

#### Scenario: Missing zone treated as zero share

- **WHEN** a tariff zone is omitted from the provided zone shares while the remaining shares sum to 100%
- **THEN** the omitted zone is treated as a 0% share and contributes no energy or distribution cost

### Requirement: Apply per-kWh surcharges to all energy

The system SHALL apply the quality fee, renewable-energy (OZE) fee, and cogeneration fee to the full total energy usage, independent of zone allocation.

#### Scenario: Surcharges scale with total energy

- **WHEN** a tariff is calculated for a given total annual energy usage
- **THEN** the surcharge component equals the total energy multiplied by the sum of the quality, OZE, and cogeneration per-kWh rates

### Requirement: Apply fixed monthly fees over the year

The system SHALL include fixed network fees and the subscription fee as monthly charges multiplied across the twelve months of the year. For now the system SHALL assume a three-phase meter and monthly billing, using the three-phase fixed network fee and the monthly subscription fee.

#### Scenario: Three-phase fixed network fee applied

- **WHEN** a tariff cost is calculated
- **THEN** the fixed network fee component uses the three-phase rate multiplied across the twelve months

#### Scenario: Monthly subscription fee applied

- **WHEN** a tariff cost is calculated
- **THEN** the subscription fee component uses the monthly per-month rate multiplied across the twelve months

### Requirement: Apply the capacity fee by annual consumption bracket

The system SHALL select the monthly capacity fee from the annual consumption bracket determined by the total annual energy usage, and include it as a monthly charge across the year.

#### Scenario: Bracket chosen from total usage

- **WHEN** the total annual energy usage falls within one of the brackets (below 500 kWh, 500–1,200 kWh, 1,200–2,800 kWh, above 2,800 kWh)
- **THEN** the capacity fee component uses the monthly rate for that bracket multiplied across the twelve months

### Requirement: Load rates from a per-year JSON catalog

The system SHALL load tariff zones, energy prices, distribution variable fees, and common fees from a static JSON file that is deserialized at runtime, and SHALL select the catalog by year so that rates for different years can coexist and a new year can be added by supplying a new JSON file without code changes.

#### Scenario: Rates loaded for a given year

- **WHEN** a calculation requests a year for which a JSON rate catalog exists
- **THEN** the system deserializes that year's catalog and uses its rates to compute costs

#### Scenario: Year without a catalog is rejected

- **WHEN** a calculation requests a year for which no JSON rate catalog is available
- **THEN** the system reports that rates for the requested year are unavailable rather than returning a cost

### Requirement: Support all five Group G tariffs with 2026 rates

The system SHALL provide a 2026 rate catalog covering all Group G tariffs (G11, G12, G12w, G13, G13s) with their zones, energy prices, and distribution variable fees taken from the documented 2026 gross rates.

#### Scenario: Each tariff is available for calculation

- **WHEN** the caller selects any of G11, G12, G12w, G13, or G13s for 2026
- **THEN** the system has a definition with the correct zones and 2026 rates available to compute its cost

#### Scenario: G13s is modeled as twelve zones

- **WHEN** the G13s definition is loaded
- **THEN** it exposes twelve zones covering the cross product of season (summer/winter), day type (workday/holiday), and period (day peak / day off-peak / night), each with its own distribution variable fee

#### Scenario: G13s has no energy price

- **WHEN** a G13s calculation is performed
- **THEN** the energy-price component is zero for every G13s zone and the cost consists of distribution variable fees, per-kWh surcharges, fixed fees, and the capacity fee

### Requirement: Compute and compare all tariffs at once

The system SHALL provide a way to compute the cost of every Group G tariff in a single call, given the shared year, total energy, and the per-tariff zone share splits.

#### Scenario: Calculate all tariffs

- **WHEN** the caller supplies the year, total energy, and zone shares for each tariff
- **THEN** the system returns a cost result for every tariff, using the requested year's rates, so they can be compared

### Requirement: Return a per-component cost breakdown

The system SHALL return, for each calculated tariff, the cost broken down by component (energy, distribution variable, surcharges, fixed fees, capacity fee) in addition to the total annual cost.

#### Scenario: Breakdown components sum to the total

- **WHEN** a tariff cost is returned
- **THEN** the breakdown exposes the energy, distribution, surcharge, fixed-fee, and capacity-fee components, and their sum equals the reported total annual cost
