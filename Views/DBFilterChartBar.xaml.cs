using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DBFilterChartBar : UserControl
    {
        public DBFilterChartBar()
        {
            this.InitializeComponent();
        }


        public DBFilterChart FilterChart { get; set; }

        public double MaxDomain { get; set; } = 0;

        public double ItemCount { get; set; } = 0;

        void xDomain_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result;
            if (double.TryParse(xDomain.Text, out result))
            {
                MaxDomain = result;
                FilterChart.UpdateChart();
            }
            FilterChart.UpdateFilter();
        }

        public bool IsSelected { get; set; }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((xBar.Background as SolidColorBrush)?.Color == Colors.SteelBlue)
                xBar.Background = new SolidColorBrush(Colors.Salmon);
            else xBar.Background = new SolidColorBrush(Colors.SteelBlue);
            IsSelected = ((xBar.Background as SolidColorBrush)?.Color == Colors.Salmon);
            FilterChart.UpdateFilter();
        }
    }
}
