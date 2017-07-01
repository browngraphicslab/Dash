using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public class BoolToVisibilityConverter : SafeDataToXamlConverter<bool, Visibility>
    {
        public override Visibility ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? Visibility.Visible : Visibility.Collapsed;
        }

        public override bool ConvertXamlToData(Visibility xaml, object parameter = null)
        {
            return xaml == Visibility.Visible;
        }
    }
}
