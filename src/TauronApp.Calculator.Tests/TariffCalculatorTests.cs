using TauronApp.Calculator;
using TauronApp.Calculator.Domain;

namespace TauronApp.Calculator.Tests;

public sealed class TariffCalculatorTests
{
    private const int Year2026 = 2026;
    private readonly TariffCalculator _sut = new();

    // ── 5.1 G11 single-zone hand-computed total ──────────────────────────────

    [Fact]
    public void G11_SingleZone_ReturnsCorrectTotal()
    {
        // 2000 kWh, 100% AllDay (2026 rates)
        // energyCost        = 2000 × 0.6175          = 1235.00
        // distributionCost  = 2000 × 0.3031          =  606.20
        // surcharges        = 2000 × (0.0407+0.0090+0.0037) = 106.80
        // fixedFees         = 12 × (13.36 + 5.61)    =  227.64
        // capacityFee       = 12 × 21.13             =  253.56  (1200–2800 bracket)
        // total             =                          2429.20

        var input = new CalculationInput(
            Year2026,
            2000m,
            new Dictionary<TariffZone, decimal> { [TariffZone.AllDay] = 100m });

        var result = _sut.Calculate(TariffId.G11, input);

        Assert.Equal(1235.00m, result.EnergyCost);
        Assert.Equal(606.20m, result.DistributionCost);
        Assert.Equal(106.80m, result.Surcharges);
        Assert.Equal(227.64m, result.FixedFees);
        Assert.Equal(253.56m, result.CapacityFee);
        Assert.Equal(2429.20m, result.Total);
    }

    // ── 5.2 G12 multi-zone percentage allocation ─────────────────────────────

    [Fact]
    public void G12_MultiZone_AllocatesEnergyByPercentage()
    {
        // 1000 kWh: 60% Day, 40% Night
        // Day energy   = 600, Night energy = 400
        // energyCost   = 600×0.6740 + 400×0.5141 = 404.40 + 205.64 = 610.04
        // distCost     = 600×0.3494 + 400×0.0686 = 209.64 +  27.44 = 237.08

        var input = new CalculationInput(
            Year2026,
            1000m,
            new Dictionary<TariffZone, decimal>
            {
                [TariffZone.Day] = 60m,
                [TariffZone.Night] = 40m
            });

        var result = _sut.Calculate(TariffId.G12, input);

        Assert.Equal(610.04m, result.EnergyCost);
        Assert.Equal(237.08m, result.DistributionCost);
    }

    // ── 5.3 Validation ───────────────────────────────────────────────────────

    [Fact]
    public void Validation_RejectsSharesThatDontSumTo100()
    {
        var input = new CalculationInput(
            Year2026,
            1000m,
            new Dictionary<TariffZone, decimal>
            {
                [TariffZone.Day] = 60m,
                [TariffZone.Night] = 30m   // sums to 90, not 100
            });

        Assert.Throws<ArgumentException>(() => _sut.Calculate(TariffId.G12, input));
    }

    [Fact]
    public void Validation_AcceptsOneDecimalPlaceSharesSummingToExactly100()
    {
        var input = new CalculationInput(
            Year2026,
            1000m,
            new Dictionary<TariffZone, decimal>
            {
                [TariffZone.Day] = 99.9m,
                [TariffZone.Night] = 0.1m
            });

        var ex = Record.Exception(() => _sut.Calculate(TariffId.G12, input));
        Assert.Null(ex);
    }

    [Fact]
    public void Validation_RejectsSharesWithMoreThanOneDecimalPlace()
    {
        var input = new CalculationInput(
            Year2026,
            1000m,
            new Dictionary<TariffZone, decimal>
            {
                [TariffZone.Day] = 66.67m,
                [TariffZone.Night] = 33.33m
            });

        Assert.Throws<ArgumentException>(() => _sut.Calculate(TariffId.G12, input));
    }

    [Fact]
    public void Validation_OmittedZoneTreatedAsZeroShare()
    {
        // Only AllDay supplied with 100% — valid for G11
        var input = new CalculationInput(
            Year2026,
            1000m,
            new Dictionary<TariffZone, decimal> { [TariffZone.AllDay] = 100m });

        var ex = Record.Exception(() => _sut.Calculate(TariffId.G11, input));
        Assert.Null(ex);
    }

    // ── 5.4 Surcharges, fixed fees, capacity fee brackets ────────────────────

    [Fact]
    public void Surcharges_ScaleWithTotalEnergy()
    {
        var input = new CalculationInput(
            Year2026,
            1000m,
            new Dictionary<TariffZone, decimal> { [TariffZone.AllDay] = 100m });

        var result = _sut.Calculate(TariffId.G11, input);

        // 1000 × (0.0407 + 0.0090 + 0.0037) = 1000 × 0.0534 = 53.40
        Assert.Equal(53.40m, result.Surcharges);
    }

    [Fact]
    public void FixedFees_UseThreePhaseAndMonthlyRates()
    {
        var input = new CalculationInput(
            Year2026,
            1000m,
            new Dictionary<TariffZone, decimal> { [TariffZone.AllDay] = 100m });

        var result = _sut.Calculate(TariffId.G11, input);

        // 12 × (13.36 + 5.61) = 12 × 18.97 = 227.64
        Assert.Equal(227.64m, result.FixedFees);
    }

    [Theory]
    [InlineData(400, 5.28)]    // below 500
    [InlineData(500, 12.68)]   // 500–1200
    [InlineData(1200, 21.13)]   // 1200–2800
    [InlineData(2800, 29.58)]   // above 2800
    [InlineData(5000, 29.58)]   // well above 2800
    public void CapacityFee_SelectsCorrectBracket(double totalKwhDouble, double expectedMonthlyRateDouble)
    {
        var totalKwh = (decimal)totalKwhDouble;
        var expectedMonthlyRate = (decimal)expectedMonthlyRateDouble;

        var input = new CalculationInput(
            Year2026,
            totalKwh,
            new Dictionary<TariffZone, decimal> { [TariffZone.AllDay] = 100m });

        var result = _sut.Calculate(TariffId.G11, input);

        Assert.Equal(expectedMonthlyRate * 12m, result.CapacityFee);
    }

    // ── 5.5 G13s — 12 zones, zero energy price ───────────────────────────────

    [Fact]
    public void G13s_HasTwelveZones()
    {
        var provider = new TauronApp.Calculator.Catalog.RateCatalogProvider();
        var catalog = provider.GetCatalog(Year2026);
        var g13s = catalog.Tariffs[TariffId.G13s];

        Assert.Equal(12, g13s.Zones.Count);
    }

    [Fact]
    public void G13s_ZeroEnergyPrice_DistributionOnlyCost()
    {
        // 11 zones at 8.3% + 1 zone at 8.7% = 100.0% (all 1 decimal place)
        var input = new CalculationInput(Year2026, 2400m, G13sShares());
        var result = _sut.Calculate(TariffId.G13s, input);

        Assert.Equal(0m, result.EnergyCost);
        Assert.True(result.DistributionCost > 0m);
    }

    private static IReadOnlyDictionary<TariffZone, decimal> G13sShares() =>
        new Dictionary<TariffZone, decimal>
        {
            [TariffZone.SummerWorkdayDayPeak]    = 8.3m,
            [TariffZone.SummerWorkdayDayOffPeak] = 8.3m,
            [TariffZone.SummerWorkdayNight]      = 8.3m,
            [TariffZone.SummerHolidayDayPeak]    = 8.3m,
            [TariffZone.SummerHolidayDayOffPeak] = 8.3m,
            [TariffZone.SummerHolidayNight]      = 8.3m,
            [TariffZone.WinterWorkdayDayPeak]    = 8.3m,
            [TariffZone.WinterWorkdayDayOffPeak] = 8.3m,
            [TariffZone.WinterWorkdayNight]      = 8.3m,
            [TariffZone.WinterHolidayDayPeak]    = 8.3m,
            [TariffZone.WinterHolidayDayOffPeak] = 8.3m,
            [TariffZone.WinterHolidayNight]      = 8.7m, // 11×8.3 + 8.7 = 100.0
        };

    // ── 5.6 Catalog loading ───────────────────────────────────────────────────

    [Fact]
    public void Catalog_LoadsFor2026()
    {
        var provider = new TauronApp.Calculator.Catalog.RateCatalogProvider();
        var catalog = provider.GetCatalog(Year2026);

        Assert.Equal(2026, catalog.Year);
        Assert.Equal(5, catalog.Tariffs.Count);
    }

    [Fact]
    public void Catalog_ThrowsForUnavailableYear()
    {
        var provider = new TauronApp.Calculator.Catalog.RateCatalogProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetCatalog(1999));
    }

    // ── 5.7 CalculateAll ─────────────────────────────────────────────────────

    [Fact]
    public void CalculateAll_ReturnsFiveTariffs()
    {
        var allShares = new Dictionary<TariffId, IReadOnlyDictionary<TariffZone, decimal>>
        {
            [TariffId.G11] = new Dictionary<TariffZone, decimal> { [TariffZone.AllDay] = 100m },
            [TariffId.G12] = new Dictionary<TariffZone, decimal> { [TariffZone.Day] = 70m, [TariffZone.Night] = 30m },
            [TariffId.G12w] = new Dictionary<TariffZone, decimal> { [TariffZone.Peak] = 60m, [TariffZone.OffPeak] = 40m },
            [TariffId.G13] = new Dictionary<TariffZone, decimal> { [TariffZone.MorningPeak] = 30m, [TariffZone.EveningPeak] = 30m, [TariffZone.Remaining] = 40m },
            [TariffId.G13s] = G13sShares()
        };

        var results = _sut.CalculateAll(Year2026, 2000m, allShares);

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public void CalculateAll_ComponentsSumToTotal()
    {
        var allShares = new Dictionary<TariffId, IReadOnlyDictionary<TariffZone, decimal>>
        {
            [TariffId.G11] = new Dictionary<TariffZone, decimal> { [TariffZone.AllDay] = 100m },
            [TariffId.G12] = new Dictionary<TariffZone, decimal> { [TariffZone.Day] = 70m, [TariffZone.Night] = 30m },
            [TariffId.G12w] = new Dictionary<TariffZone, decimal> { [TariffZone.Peak] = 60m, [TariffZone.OffPeak] = 40m },
            [TariffId.G13] = new Dictionary<TariffZone, decimal> { [TariffZone.MorningPeak] = 30m, [TariffZone.EveningPeak] = 30m, [TariffZone.Remaining] = 40m },
            [TariffId.G13s] = G13sShares()
        };

        var results = _sut.CalculateAll(Year2026, 2000m, allShares);

        foreach (var breakdown in results)
        {
            var expected = breakdown.EnergyCost + breakdown.DistributionCost
                           + breakdown.Surcharges + breakdown.FixedFees + breakdown.CapacityFee;
            Assert.Equal(expected, breakdown.Total);
        }
    }
}
