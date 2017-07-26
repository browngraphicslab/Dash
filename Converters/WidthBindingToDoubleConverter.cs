using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using static Dash.DocumentViewModel;

namespace Dash
{
    class WidthBindingToDoubleConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var binding = (WidthAndMenuOpenWrapper) value;
            return binding.MenuOpen ? binding.Width + 55 : binding.Width;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new WidthAndMenuOpenWrapper() { Width = (double) value };
        }
    }
}
