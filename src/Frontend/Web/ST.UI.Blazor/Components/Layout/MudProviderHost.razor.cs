using MudBlazor;

namespace ST.UI.Blazor.Components.Layout;

public partial class MudProviderHost
{
    private static readonly MudTheme EnterpriseTheme = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#334E68",
            Secondary = "#52606D",
            Tertiary = Colors.Green.Darken4,
            Warning = Colors.Orange.Darken4,
            Error = Colors.Red.Darken4,
            Info = Colors.LightBlue.Darken4,
            Success = Colors.Green.Darken4,
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1E293B",
            Background = "#F5F6F8",       // Neutral application canvas.
            DrawerBackground = "#FFFFFF",
            Surface = "#FFFFFF",
            TextPrimary = "#1E293B",      // Deep Slate for text
            TextSecondary = "#52606D",
            ActionDefault = "#52606D",
            Divider = "#E2E8F0",
            TableLines = "#E2E8F0"
        },
        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "6px" // Compact shared controls.
        },
        Typography = new Typography()
        {
            Default = new DefaultTypography()
            {
                FontFamily = ["Inter", "Segoe UI", "sans-serif"],
                FontSize = ".8125rem",
                LineHeight = "1.5"
            },
            H1 = new H1Typography()
            {
                FontWeight = "700",
                FontSize = "2.25rem"
            },
            H2 = new H2Typography()
            {
                FontWeight = "600",
                FontSize = "1.875rem"
            },
            H3 = new H3Typography()
            {
                FontWeight = "600",
                FontSize = "1.5rem"
            },
            H4 = new H4Typography()
            {
                FontWeight = "600",
                FontSize = "1.25rem"
            },
            H5 = new H5Typography()
            {
                FontWeight = "600",
                FontSize = "1.125rem"
            },
            H6 = new H6Typography()
            {
                FontWeight = "600",
                FontSize = "1rem"
            },
            Subtitle1 = new Subtitle1Typography()
            {
                FontSize = ".875rem"
            },
            Subtitle2 = new Subtitle2Typography()
            {
                FontSize = ".8125rem"
            },
            Body1 = new Body1Typography()
            {
                FontSize = ".875rem"
            },
            Body2 = new Body2Typography()
            {
                FontSize = ".8125rem"
            },
            Button = new ButtonTypography()
            {
                TextTransform = "none",
                FontWeight = "500",
                FontSize = ".8125rem"
            }
        }
    };
}
