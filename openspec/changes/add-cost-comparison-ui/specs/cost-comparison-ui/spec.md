## ADDED Requirements

### Requirement: Collect global calculation inputs

The home screen SHALL let the user provide the global values required to calculate energy costs: the billing year and the total annual energy usage in kWh. These values SHALL apply to every tariff in the comparison.

#### Scenario: User enters global inputs

- **WHEN** the user enters a billing year and a total annual energy usage on the home screen
- **THEN** those values are used as the year and total energy for calculating every tariff's cost

#### Scenario: Total energy must be valid

- **WHEN** the total annual energy usage is missing or not a non-negative number
- **THEN** the screen reports the invalid input and does not present tariff costs computed from it

### Requirement: Provide zone shares for every zone of every tariff

The screen SHALL let the user provide the percentage share for each zone of each Group G tariff (G11, G12, G12w, G13, G13s), exposing exactly the zones that belong to each tariff. The set of zones offered for a tariff SHALL match that tariff's definition in the calculation engine.

#### Scenario: All zones of a tariff are editable

- **WHEN** the user opens a tariff's inputs
- **THEN** the screen shows one share input for each zone that belongs to that tariff (for example one for G11, two for G12, and twelve for G13s)

#### Scenario: Shares are seeded with valid defaults

- **WHEN** the home screen first loads
- **THEN** every tariff is pre-populated with default zone shares that sum to 100%, so an initial cost is shown for each tariff without further input

#### Scenario: Per-tariff share validation

- **WHEN** a tariff's zone shares do not sum to exactly 100%, or a share has more than one decimal place
- **THEN** that tariff shows an inline validation message and no cost, while tariffs whose shares are valid still show their costs

### Requirement: Show a cost card per tariff

The screen SHALL display one card per Group G tariff. Each card SHALL show the tariff's name and its overall annual cost. Cards SHALL be ordered by total annual cost ascending so the cheapest tariff is easy to identify.

#### Scenario: Card shows tariff and total

- **WHEN** a tariff's inputs are valid
- **THEN** its card shows the tariff name and the overall annual cost for the given year, total energy, and zone shares

#### Scenario: Cheapest tariff is identifiable

- **WHEN** all tariffs have valid inputs and costs
- **THEN** the cards are ordered from lowest to highest total annual cost and the cheapest is visually distinguished

### Requirement: Expand a card to reveal the cost breakdown

Each tariff card SHALL be collapsed by default showing only the total, and SHALL expand when clicked to reveal the per-component cost breakdown returned by the engine (energy, distribution, surcharges, fixed fees, capacity fee) together with that tariff's zone-share inputs.

#### Scenario: Click expands the card

- **WHEN** the user clicks a collapsed tariff card
- **THEN** the card expands to show the cost breakdown by component and the zone-share inputs for that tariff

#### Scenario: Breakdown components sum to the total

- **WHEN** a card is expanded for a tariff with valid inputs
- **THEN** the displayed energy, distribution, surcharge, fixed-fee, and capacity-fee components sum to the overall annual cost shown on the card

#### Scenario: Click collapses an expanded card

- **WHEN** the user clicks an expanded tariff card
- **THEN** the card collapses back to showing only the tariff name and overall cost

### Requirement: Recalculate live as inputs change

The screen SHALL recompute and update the displayed costs whenever the user changes the year, total energy, or any zone share, without requiring a separate submit action.

#### Scenario: Editing an input updates results

- **WHEN** the user changes the total energy, year, or any zone share
- **THEN** the affected tariff cards and the comparison chart update to reflect the new values

### Requirement: Compare tariffs with a column chart

The screen SHALL display, below the cards, a column chart comparing the total annual cost of each tariff, with one column per tariff labeled by tariff name.

#### Scenario: Chart reflects all tariffs

- **WHEN** the tariffs have been calculated
- **THEN** the column chart shows one column per tariff whose height corresponds to that tariff's total annual cost

#### Scenario: Chart updates with inputs

- **WHEN** the user changes any input that affects a tariff's cost
- **THEN** the corresponding column in the chart updates to match the new total
