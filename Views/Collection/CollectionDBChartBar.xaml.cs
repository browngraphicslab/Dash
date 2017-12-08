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
    public sealed partial class CollectionDBChartBar : UserControl
    {
        public CollectionDBChartBar()
        {
            this.InitializeComponent();
        }
        public int BucketIndex { get; set; }

        public CollectionDBView FilterChart { get; set; }

        double _maxDomain = 0;
        public double MaxDomain
        {
            get { return _maxDomain; }
            set
            {
                _maxDomain = value;
                if (xDomain.Text != _maxDomain.ToString())
                    xDomain.Text = _maxDomain.ToString();
            }
        }

        public double ItemCount { get; set; } = 0;

        void xDomain_TextChanged(object sender, TextChangedEventArgs e)
        {
            double result;
            if (double.TryParse(xDomain.Text, out result))
            {
                if (result != MaxDomain)
                {
                    FilterChart.UpdateBucket(BucketIndex, result);
                }
            }
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(CollectionDBChartBar), new PropertyMetadata(default(bool)));
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IsSelected = !IsSelected;
            FilterChart.UpdateSelection(BucketIndex, IsSelected);
        }
    }
    //bcz: temporary hack --- need to just convert bool to Bold for use anywhere, not just SchemaHeaders
    public class BoolToHighlightConverter : SafeDataToXamlConverter<bool, Brush>
    {
        public override Brush ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? new SolidColorBrush(Colors.LightSalmon) : new SolidColorBrush(Colors.Gainsboro);
        }

        public override bool ConvertXamlToData(Brush xaml, object parameter = null)
        {
            throw new System.Exception();
            //return xaml.Equals(FontWeights.ExtraBold);
        }
    }
}
