namespace TauronApp.Calculator.Domain;

public sealed record TariffDefinition(
    TariffId Id,
    IReadOnlyList<TariffZoneRate> Zones);
