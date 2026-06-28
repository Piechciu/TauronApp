namespace TauronApp.Calculator.Domain;

public sealed record CostBreakdown(
    TariffId TariffId,
    decimal EnergyCost,
    decimal DistributionCost,
    decimal Surcharges,
    decimal FixedFees,
    decimal CapacityFee)
{
    public decimal Total => EnergyCost + DistributionCost + Surcharges + FixedFees + CapacityFee;
}
