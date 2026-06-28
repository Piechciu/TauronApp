namespace TauronApp.Calculator.Domain;

public sealed record TariffZoneRate(
    TariffZone Zone,
    decimal EnergyPricePlnPerKwh,
    decimal DistributionVariablePlnPerKwh);
