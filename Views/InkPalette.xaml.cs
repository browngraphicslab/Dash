using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Color = Windows.UI.Color;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class InkPalette : UserControl
    {
        private MainPage _page;

        private Rectangle _selectedRectangle;
        private double _opacity;

        public InkPalette(MainPage page)
        {
            this.InitializeComponent();
            _page = page;
            InitializeColors();
        }


        private void InitializeColors()
        {
            AddColorRange(Colors.Red, Colors.Violet);
            AddColorRange(Colors.Violet, Colors.Blue);
            AddColorRange(Colors.Blue, Colors.Aqua);
            AddColorRange(Colors.Aqua, Colors.Green);
            AddColorRange(Colors.Green, Colors.Yellow);
        }

        private void AddColorRange(Color color1, Color color2)
        {
            int r1 = color1.R;
            int rEnd = color2.R;
            int b1 = color1.B;
            int bEnd = color2.B;
            int g1 = color1.G;
            int gEnd = color2.G;
            for (byte i = 0; i < 25; i++)
            {
                var rAverage = r1 + (int)((rEnd - r1) * i / 25);
                var gAverage = g1 + (int)((gEnd - g1) * i / 25);
                var bAverage = b1 + (int)((bEnd - b1) * i / 25);
                AddColorRect(Color.FromArgb(255, (byte)rAverage, (byte)gAverage, (byte)bAverage));
            }
        }

        private void AddColorRect(Color color)
        {
            Rectangle rect = new Rectangle();
            rect.Width = 5;
            rect.Height = 50;
            rect.Fill = new SolidColorBrush(color);
            rect.Tapped += RectOnTapped;
            XStackPanel.Children.Add(rect);
        }


        private void RectOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            Rectangle rect = sender as Rectangle;
            if (!rect.Equals(_selectedRectangle))
            {
                if(_selectedRectangle!=null) _selectedRectangle.StrokeThickness = 0;
                rect.Stroke = new SolidColorBrush(Colors.White);
                rect.StrokeThickness = 1;
                _selectedRectangle = rect;
            }
            xExampleRect.Fill = _selectedRectangle.Fill;
            _page.ChangeInkColor(rect?.Fill is SolidColorBrush ? ((SolidColorBrush) rect.Fill).Color : new Color());
        }

        private void xOpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            _opacity = e.NewValue;
            foreach (UIElement item in XStackPanel.Children)
            {
                var rect = item as Rectangle;
                if (rect?.Fill != null) rect.Fill.Opacity = _opacity;
            }
            _page.ChangeInkColor(_selectedRectangle?.Fill is SolidColorBrush ? ((SolidColorBrush)_selectedRectangle.Fill).Color : new Color());
        }
    }
}
