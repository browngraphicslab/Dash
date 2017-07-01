using System;
using DashShared;

namespace Dash
{
    public class VisibilityToVisibilityConverter : SafeDataToXamlConverter<Visibility, Windows.UI.Xaml.Visibility>
    {
        static VisibilityToVisibilityConverter()
        {
            Instance = new VisibilityToVisibilityConverter();
        }

        public static VisibilityToVisibilityConverter Instance { get; private set; }

        public override Windows.UI.Xaml.Visibility ConvertDataToXaml(Visibility data, object parameter = null)
        {
            switch (data)
            {
                case Visibility.Visible:
                    return Windows.UI.Xaml.Visibility.Visible;
                case Visibility.Collapsed:
                    return Windows.UI.Xaml.Visibility.Collapsed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(data), data, null);
            }
        }

        public override Visibility ConvertXamlToData(Windows.UI.Xaml.Visibility xaml, object parameter = null)
        {
            switch (xaml)
            {
                case Windows.UI.Xaml.Visibility.Visible:
                    return Visibility.Visible;
                case Windows.UI.Xaml.Visibility.Collapsed:
                    return Visibility.Collapsed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(xaml), xaml, null);
            }
        }
    }
}