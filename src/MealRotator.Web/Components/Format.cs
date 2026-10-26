using System.Globalization;

namespace MealRotator.Web.Components;

/// <summary>Dutch display texts for dates and counts (nl-BE culture).</summary>
public static class Format
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>"wo 21 okt"</summary>
    public static string DayShort(DateOnly date) => date.ToString("ddd d MMM");

    /// <summary>"21 okt 2026"</summary>
    public static string DateLong(DateOnly date) => date.ToString("d MMM yyyy");

    /// <summary>"Woensdag"</summary>
    public static string DayName(DateOnly date) => Capitalize(date.ToString("dddd"));

    /// <summary>"12 okt — 18 okt 2026"</summary>
    public static string WeekRange(DateOnly start) =>
        $"{start:d MMM} — {start.AddDays(6):d MMM yyyy}";

    /// <summary>"Nog nooit" / "Vandaag" / "Gisteren" / "12 dagen geleden"</summary>
    public static string Ago(DateOnly? lastEaten)
    {
        if (lastEaten is null)
            return "Nog nooit";

        var days = DaysSince(lastEaten.Value);
        return days switch
        {
            <= 0 => "Vandaag",
            1 => "Gisteren",
            _ => $"{days} dagen geleden"
        };
    }

    /// <summary>Short variant for suggestion chips: "nooit" / "vandaag" / "32 d".</summary>
    public static string AgoShort(DateOnly? lastEaten)
    {
        if (lastEaten is null)
            return "nooit";

        var days = DaysSince(lastEaten.Value);
        return days <= 0 ? "vandaag" : $"{days} d";
    }

    public static int DaysSince(DateOnly date) => Today.DayNumber - date.DayNumber;

    /// <summary>"Nog nooit" / "1 keer" / "3 keer"</summary>
    public static string TimesEaten(int count) => count == 0 ? "Nog nooit" : $"{count} keer";

    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpper(text[0], CultureInfo.CurrentCulture) + text[1..];
}
