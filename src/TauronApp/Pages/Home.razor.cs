using Microsoft.AspNetCore.Components;
using TauronApp.Calculator;
using TauronApp.Calculator.Domain;

namespace TauronApp.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    private TariffCalculator Calculator { get; set; } = default!;

    private int _year = 2026;
    private decimal? _totalEnergyKwh = 3500m;

    private IReadOnlyDictionary<TariffId, IReadOnlyList<TariffZone>> _zonesByTariff =
        new Dictionary<TariffId, IReadOnlyList<TariffZone>>();

    private readonly Dictionary<TariffId, Dictionary<TariffZone, decimal>> _shares = new();
    private readonly Dictionary<TariffId, TariffResult> _results = new();
    private readonly HashSet<TariffId> _expanded = new();

    private IReadOnlyList<TariffResult> _ordered = Array.Empty<TariffResult>();

    private bool GlobalInputValid => _totalEnergyKwh is >= 0m;

    protected override void OnInitialized()
    {
        _zonesByTariff = Calculator.GetTariffZones(_year);
        SeedDefaultShares();
        Recalculate();
    }

    /// <summary>
    /// Seeds every tariff with an even zone-share split that sums to exactly 100%
    /// (with at most one decimal place), so an initial cost shows for each tariff.
    /// </summary>
    private void SeedDefaultShares()
    {
        foreach (var (tariff, zones) in _zonesByTariff)
        {
            var shares = new Dictionary<TariffZone, decimal>();
            var count = zones.Count;
            var each = Math.Round(100m / count, 1, MidpointRounding.AwayFromZero);

            foreach (var zone in zones)
                shares[zone] = each;

            // Push any rounding remainder onto the first zone so the total is exactly 100%.
            shares[zones[0]] += 100m - (each * count);

            _shares[tariff] = shares;
        }
    }

    private void OnYearChanged(int year)
    {
        _year = year;
        _zonesByTariff = Calculator.GetTariffZones(_year);
        Recalculate();
    }

    private void OnTotalEnergyChanged(decimal? value)
    {
        _totalEnergyKwh = value;
        Recalculate();
    }

    private void OnShareChanged(TariffId tariff, TariffZone zone, decimal value)
    {
        _shares[tariff][zone] = value;
        Recalculate();
    }

    private void ToggleExpanded(TariffId tariff)
    {
        if (!_expanded.Remove(tariff))
            _expanded.Add(tariff);
    }

    private bool IsExpanded(TariffId tariff) => _expanded.Contains(tariff);

    private decimal ShareSum(TariffId tariff) => _shares[tariff].Values.Sum();

    private void Recalculate()
    {
        _results.Clear();

        foreach (var (tariff, shares) in _shares)
        {
            var sharesValid = SharesValid(shares);

            if (!GlobalInputValid)
            {
                _results[tariff] = TariffResult.Invalid(tariff, "Enter a valid total energy usage.");
                continue;
            }

            if (!sharesValid)
            {
                _results[tariff] = TariffResult.Invalid(
                    tariff,
                    $"Zone shares must sum to exactly 100% (currently {ShareSum(tariff):0.#}%) with at most one decimal place.");
                continue;
            }

            try
            {
                var input = new CalculationInput(_year, _totalEnergyKwh!.Value, shares);
                var breakdown = Calculator.Calculate(tariff, input);
                _results[tariff] = TariffResult.Ok(tariff, breakdown);
            }
            catch (ArgumentException ex)
            {
                _results[tariff] = TariffResult.Invalid(tariff, ex.Message);
            }
        }

        // Valid tariffs first, cheapest to most expensive; invalid ones last.
        _ordered = _results.Values
            .OrderBy(r => r.IsValid ? 0 : 1)
            .ThenBy(r => r.Breakdown?.Total ?? decimal.MaxValue)
            .ToList();
    }

    private static bool SharesValid(IReadOnlyDictionary<TariffZone, decimal> shares)
    {
        var total = 0m;
        foreach (var share in shares.Values)
        {
            if (share * 10m != Math.Floor(share * 10m))
                return false;
            total += share;
        }

        return total == 100m;
    }

    private IReadOnlyList<TariffResult> ValidResults =>
        _ordered.Where(r => r.IsValid).ToList();

    private IReadOnlyList<ChartPoint> ChartData =>
        ValidResults.Select(r => new ChartPoint(TariffName(r.TariffId), r.Breakdown!.Total)).ToList();

    private TariffId? CheapestTariff =>
        ValidResults.Count > 0 ? ValidResults[0].TariffId : null;

    private static string TariffName(TariffId tariff) => tariff.ToString();

    private static string Money(decimal value) => value.ToString("N2") + " zł";

    // Tariffs with many zones (G13s) are grouped by season/day-type so the share
    // inputs read as a small grid of periods instead of a flat list of twelve fields.
    private static readonly string[] PeriodSuffixes = { "DayOffPeak", "DayPeak", "Night" };

    private bool IsGrouped(TariffId tariff) => _zonesByTariff[tariff].Count > 3;

    private IEnumerable<IGrouping<string, TariffZone>> ZoneGroups(TariffId tariff) =>
        _zonesByTariff[tariff].GroupBy(GroupLabel);

    private static string GroupLabel(TariffZone zone)
    {
        var raw = zone.ToString();
        foreach (var suffix in PeriodSuffixes)
            if (raw.EndsWith(suffix, StringComparison.Ordinal))
                return Humanize(raw[..^suffix.Length]);

        return Humanize(raw);
    }

    private static string PeriodLabel(TariffZone zone)
    {
        var raw = zone.ToString();
        foreach (var suffix in PeriodSuffixes)
            if (raw.EndsWith(suffix, StringComparison.Ordinal))
                return Humanize(suffix);

        return Humanize(raw);
    }

    /// <summary>Turns a PascalCase zone enum name into spaced words for display.</summary>
    private static string ZoneName(TariffZone zone) => Humanize(zone.ToString());

    private static string Humanize(string raw)
    {
        var chars = new List<char>(raw.Length + 4);
        for (var i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i]) && !char.IsUpper(raw[i - 1]))
                chars.Add(' ');
            chars.Add(raw[i]);
        }

        return new string(chars.ToArray());
    }

    public sealed record ChartPoint(string Tariff, decimal Total);

    private sealed record TariffResult(TariffId TariffId, CostBreakdown? Breakdown, string? Error)
    {
        public bool IsValid => Breakdown is not null;

        public static TariffResult Ok(TariffId tariff, CostBreakdown breakdown) => new(tariff, breakdown, null);

        public static TariffResult Invalid(TariffId tariff, string error) => new(tariff, null, error);
    }
}
