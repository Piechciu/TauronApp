namespace TauronApp.Calculator.Domain;

public sealed record CalculationInput(
    int Year,
    decimal TotalEnergyKwh,
    IReadOnlyDictionary<TariffZone, decimal> ZoneShares);
