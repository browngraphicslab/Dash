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
            return new SolidColorBrush(HexToColor(hex));
        }

        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || (hex.Length != 7 && hex.Length != 9)) return Colors.White;
            bool useAlpha = hex.Length == 9;

            hex = hex.Substring(1);
            byte a;
            int offset = 0;
            if (useAlpha)
            {
                a = (byte)Convert.ToUInt32(hex.Substring(offset, 2), 16);
                offset += 2;
            }
            else
            {
                a = byte.MaxValue;
            }

            var r = (byte)Convert.ToUInt32(hex.Substring(offset, 2), 16);
            offset += 2;
            var g = (byte)Convert.ToUInt32(hex.Substring(offset, 2), 16);
            offset += 2;
            var b = (byte)Convert.ToUInt32(hex.Substring(offset, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
