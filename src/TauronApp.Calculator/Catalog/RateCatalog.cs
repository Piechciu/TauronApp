using TauronApp.Calculator.Domain;

namespace TauronApp.Calculator.Catalog;

internal sealed record RateCatalog(
    int Year,
    IReadOnlyDictionary<TariffId, TariffDefinition> Tariffs,
    CommonFees CommonFees);

internal sealed record CommonFees(
    decimal FixedNetworkFeeThreePhasePlnPerMonth,
    decimal SubscriptionFeeMonthlyPlnPerMonth,
    decimal QualityFeePlnPerKwh,
    decimal OzeFeePlnPerKwh,
    decimal CogenerationFeePlnPerKwh,
    IReadOnlyList<CapacityBracket> CapacityFeeBrackets);

internal sealed record CapacityBracket(
    decimal MinKwh,
    decimal? MaxKwh,
    decimal MonthlyFeePln);
