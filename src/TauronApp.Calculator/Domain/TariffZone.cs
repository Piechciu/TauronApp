namespace TauronApp.Calculator.Domain;

public enum TariffZone
{
    // G11
    AllDay,

    // G12
    Day,
    Night,

    // G12w
    Peak,
    OffPeak,

    // G13
    MorningPeak,
    EveningPeak,
    Remaining,

    // G13s — summer × workday
    SummerWorkdayDayPeak,
    SummerWorkdayDayOffPeak,
    SummerWorkdayNight,

    // G13s — summer × holiday
    SummerHolidayDayPeak,
    SummerHolidayDayOffPeak,
    SummerHolidayNight,

    // G13s — winter × workday
    WinterWorkdayDayPeak,
    WinterWorkdayDayOffPeak,
    WinterWorkdayNight,

    // G13s — winter × holiday
    WinterHolidayDayPeak,
    WinterHolidayDayOffPeak,
    WinterHolidayNight
}
