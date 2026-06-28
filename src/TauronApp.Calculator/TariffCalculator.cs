using TauronApp.Calculator.Catalog;
using TauronApp.Calculator.Domain;

namespace TauronApp.Calculator;

public sealed class TariffCalculator
{
    private const decimal ShareTolerance = 0.01m; // 1% tolerance for rounding

    private readonly RateCatalogProvider _catalogProvider;

    public TariffCalculator() : this(new RateCatalogProvider()) { }

    internal TariffCalculator(RateCatalogProvider catalogProvider)
    {
        _catalogProvider = catalogProvider;
    }

    public CostBreakdown Calculate(TariffId tariffId, CalculationInput input)
    {
        var catalog = _catalogProvider.GetCatalog(input.Year);
        var tariff = catalog.Tariffs[tariffId];

        ValidateZoneShares(tariff, input.ZoneShares);

        return ComputeBreakdown(tariffId, tariff, input.TotalEnergyKwh, input.ZoneShares, catalog.CommonFees);
    }

    public IReadOnlyList<CostBreakdown> CalculateAll(
        int year,
        decimal totalEnergyKwh,
        IReadOnlyDictionary<TariffId, IReadOnlyDictionary<TariffZone, decimal>> zoneSharesPerTariff)
    {
        var catalog = _catalogProvider.GetCatalog(year);
        var results = new List<CostBreakdown>(catalog.Tariffs.Count);

        foreach (var (tariffId, tariff) in catalog.Tariffs)
        {
            var shares = zoneSharesPerTariff.TryGetValue(tariffId, out var s)
                ? s
                : new Dictionary<TariffZone, decimal>();

            ValidateZoneShares(tariff, shares);
            results.Add(ComputeBreakdown(tariffId, tariff, totalEnergyKwh, shares, catalog.CommonFees));
        }

        return results;
    }

    private static CostBreakdown ComputeBreakdown(
        TariffId tariffId,
        TariffDefinition tariff,
        decimal totalEnergyKwh,
        IReadOnlyDictionary<TariffZone, decimal> zoneShares,
        CommonFees fees)
    {
        var energyCost = 0m;
        var distributionCost = 0m;

        foreach (var zoneRate in tariff.Zones)
        {
            var share = zoneShares.TryGetValue(zoneRate.Zone, out var s) ? s / 100m : 0m;
            var zoneEnergy = totalEnergyKwh * share;
            energyCost += zoneEnergy * zoneRate.EnergyPricePlnPerKwh;
            distributionCost += zoneEnergy * zoneRate.DistributionVariablePlnPerKwh;
        }

        var surcharges = totalEnergyKwh * (fees.QualityFeePlnPerKwh + fees.OzeFeePlnPerKwh + fees.CogenerationFeePlnPerKwh);
        var fixedFees = 12m * (fees.FixedNetworkFeeThreePhasePlnPerMonth + fees.SubscriptionFeeMonthlyPlnPerMonth);
        var capacityFee = 12m * SelectCapacityBracket(totalEnergyKwh, fees.CapacityFeeBrackets);

        return new CostBreakdown(tariffId, energyCost, distributionCost, surcharges, fixedFees, capacityFee);
    }

    private static void ValidateZoneShares(TariffDefinition tariff, IReadOnlyDictionary<TariffZone, decimal> zoneShares)
    {
        var knownZones = tariff.Zones.Select(z => z.Zone).ToHashSet();
        var total = 0m;

        foreach (var (zone, share) in zoneShares)
        {
            if (!knownZones.Contains(zone))
                throw new ArgumentException($"Zone {zone} does not belong to tariff {tariff.Id}.");
            total += share;
        }

        if (Math.Abs(total - 100m) > ShareTolerance)
            throw new ArgumentException(
                $"Zone shares for tariff {tariff.Id} sum to {total:F4}% but must be within {ShareTolerance}% of 100%.");
    }

    private static decimal SelectCapacityBracket(decimal totalEnergyKwh, IReadOnlyList<CapacityBracket> brackets)
    {
        foreach (var bracket in brackets)
        {
            if (totalEnergyKwh >= bracket.MinKwh && (bracket.MaxKwh is null || totalEnergyKwh < bracket.MaxKwh))
                return bracket.MonthlyFeePln;
        }

        return brackets[^1].MonthlyFeePln;
    }
}
