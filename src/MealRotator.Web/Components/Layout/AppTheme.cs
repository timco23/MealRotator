using MudBlazor;

namespace MealRotator.Web.Components.Layout;

/// <summary>Warm, calm colors: olive green as the primary color, terracotta as the accent.</summary>
public static class AppTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5B6B3A",
            Secondary = "#B85C38",
            Tertiary = "#C9A227",
            Info = "#3F6E7A",
            Success = "#4E7D4A",
            Warning = "#C27C0E",
            Error = "#B3261E",
            Background = "#FAF7F2",
            Surface = "#FFFFFF",
            AppbarBackground = "#FAF7F2",
            AppbarText = "#2E2A24",
            DrawerBackground = "#F3EEE6",
            DrawerText = "#3B362F",
            DrawerIcon = "#5B6B3A",
            TextPrimary = "#2E2A24",
            TextSecondary = "#6B645A",
            LinesDefault = "#E4DDD2",
            Divider = "#E4DDD2",
            TableLines = "#ECE6DC"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#A9BC7E",
            Secondary = "#E08A66",
            Tertiary = "#E3C25A",
            Info = "#8DBAC4",
            Success = "#86B57F",
            Warning = "#E0A33E",
            Error = "#F2B8B5",
            Background = "#1C1B18",
            Surface = "#26241F",
            AppbarBackground = "#1C1B18",
            AppbarText = "#ECE6DC",
            DrawerBackground = "#211F1B",
            DrawerText = "#D8D1C5",
            DrawerIcon = "#A9BC7E",
            TextPrimary = "#ECE6DC",
            TextSecondary = "#A9A195",
            LinesDefault = "#3A3731",
            Divider = "#3A3731",
            TableLines = "#34312B"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Segoe UI", "system-ui", "sans-serif"]
            },
            // Calmer than MudBlazor's default UPPERCASE button text.
            Button = new ButtonTypography
            {
                TextTransform = "none",
                FontWeight = "600"
            }
        }
    };
}
