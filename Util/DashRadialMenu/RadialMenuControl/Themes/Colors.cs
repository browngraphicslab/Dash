using System.Collections.Generic;
using Windows.UI;

namespace RadialMenuControl.Themes
{
    /// <summary>
    /// Common colors to use by default across control components
    /// </summary>
    public static class DefaultColors
    {
        public static Color InnerNormalColor = Color.FromArgb(255, 255, 255, 255),
            InnerHoverColor = Color.FromArgb(255, 139, 187, 229),
            InnerTappedColor = Color.FromArgb(255, 139, 187, 229),
            InnerReleasedColor = Color.FromArgb(255, 139, 187, 229),
            OuterNormalColor = Color.FromArgb(255, 139, 187, 229),
            OuterDisabledColor = Color.FromArgb(255, 139, 187, 229),
            OuterHoverColor = Color.FromArgb(255, 139, 187, 229),
            OuterTappedColor = Color.FromArgb(255, 139, 187, 229),
            BackgroundHighlightColor = Color.FromArgb(255, 235, 235, 235),
            ForegroundColor = Color.FromArgb(255, 241, 218, 234),
            HighlightColor = Color.FromArgb(255, 139, 187, 229),
            MeterSelectorColor = Colors.Green,
            MeterLineColor = Colors.Black;
        //Color.FromArgb(255, 64, 123, 177), DASH DARK BLUE
        //Color.FromArgb(255, 48, 97, 153), DASH DARK DARK BLUE
        // Color.FromArgb(255, 139, 187, 229), DASH LIGHT BLUE
    }
}
