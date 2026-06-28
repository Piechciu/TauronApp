using System.Text.Json.Serialization;
using TauronApp.Calculator.Domain;

namespace TauronApp.Calculator.Catalog;

// JSON shape — deserialized from tauron-rates-<year>.json

internal sealed class RateCatalogJson
{
    public int Year { get; init; }
    public List<TariffJson> Tariffs { get; init; } = [];
    public CommonFeesJson CommonFees { get; init; } = new();
}

internal sealed class TariffJson
{
    public TariffId Id { get; init; }
    public List<ZoneRateJson> Zones { get; init; } = [];
}

internal sealed class ZoneRateJson
{
    public TariffZone Zone { get; init; }
    public decimal EnergyPricePlnPerKwh { get; init; }
    public decimal DistributionVariablePlnPerKwh { get; init; }
}

internal sealed class CommonFeesJson
{
    public decimal FixedNetworkFeeThreePhasePlnPerMonth { get; init; }
    public decimal SubscriptionFeeMonthlyPlnPerMonth { get; init; }
    public decimal QualityFeePlnPerKwh { get; init; }
    public decimal OzeFeePlnPerKwh { get; init; }
    public decimal CogenerationFeePlnPerKwh { get; init; }
    public List<CapacityBracketJson> CapacityFeeBrackets { get; init; } = [];
}

internal sealed class CapacityBracketJson
{
    public decimal MinKwh { get; init; }
    public decimal? MaxKwh { get; init; }
    public decimal MonthlyFeePln { get; init; }
}
