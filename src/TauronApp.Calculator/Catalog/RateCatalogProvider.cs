using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TauronApp.Calculator.Domain;

namespace TauronApp.Calculator.Catalog;

public sealed class RateCatalogProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    private readonly ConcurrentDictionary<int, RateCatalog> _cache = new();

    internal RateCatalog GetCatalog(int year)
    {
        return _cache.GetOrAdd(year, LoadCatalog);
    }

    private static RateCatalog LoadCatalog(int year)
    {
        var assembly = typeof(RateCatalogProvider).Assembly;
        var resourceName = $"TauronApp.Calculator.Rates.tauron-rates-{year}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"No rate catalog found for year {year}. Add 'Rates/tauron-rates-{year}.json' as an EmbeddedResource.");

        var json = JsonSerializer.Deserialize<RateCatalogJson>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Rate catalog for year {year} is empty or invalid.");

        return MapToDomain(json);
    }

    private static RateCatalog MapToDomain(RateCatalogJson json)
    {
        var tariffs = json.Tariffs.ToDictionary(
            t => t.Id,
            t => new TariffDefinition(
                t.Id,
                t.Zones.Select(z => new TariffZoneRate(
                    z.Zone,
                    z.EnergyPricePlnPerKwh,
                    z.DistributionVariablePlnPerKwh)).ToList()));

        var cf = json.CommonFees;
        var commonFees = new CommonFees(
            cf.FixedNetworkFeeThreePhasePlnPerMonth,
            cf.SubscriptionFeeMonthlyPlnPerMonth,
            cf.QualityFeePlnPerKwh,
            cf.OzeFeePlnPerKwh,
            cf.CogenerationFeePlnPerKwh,
            cf.CapacityFeeBrackets.Select(b => new CapacityBracket(b.MinKwh, b.MaxKwh, b.MonthlyFeePln)).ToList());

        return new RateCatalog(json.Year, tariffs, commonFees);
    }
}
