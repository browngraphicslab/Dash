using System;
using Windows.UI;
using Windows.UI.Xaml.Media;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class ColorConverter
    {
        public static SolidColorBrush HexToBrush(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length != 7) return new SolidColorBrush(Colors.White);

            hex = hex.Substring(1);
            var a = (byte)Convert.ToUInt32("FF", 16);
            var r = (byte)Convert.ToUInt32(hex.Substring(0, 2), 16);
            var g = (byte)Convert.ToUInt32(hex.Substring(2, 2), 16);
            var b = (byte)Convert.ToUInt32(hex.Substring(4, 2), 16);

            return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
        }

        public static SolidColorBrush AlphaHexToBrush(string ahex)
        {
            if (string.IsNullOrEmpty(ahex) || ahex.Length != 9) return new SolidColorBrush(Colors.White);

            ahex = ahex.Substring(1);
            var a = (byte)Convert.ToUInt32(ahex.Substring(0, 2), 16);
            var r = (byte)Convert.ToUInt32(ahex.Substring(2, 2), 16);
            var g = (byte)Convert.ToUInt32(ahex.Substring(4, 2), 16);
            var b = (byte)Convert.ToUInt32(ahex.Substring(6, 2), 16);

            return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
        }

        public static Color HexToColor(string hex) => HexToBrush(hex).Color;

        public static Color AlphaHexToColor(string ahex) => AlphaHexToBrush(ahex).Color;
    }
}