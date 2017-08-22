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
    public sealed partial class InkSettingsPane : UserControl
    {
        public Symbol BrightnessSymbol { get; set; } = (Symbol) 0xE706;
        public Symbol SizeSymbol { get; set; } = (Symbol)0xEDA8;
        public Symbol OpacitySymbol { get; set; } = (Symbol) 0xEB42;
        public InkSettingsPane()
        {
            this.InitializeComponent();
            GlobalInkSettings.OnAttributesUpdated += (newBrush) => UpdateExample(newBrush);
            SizeSlider.Loaded += (sender, args) => SizeSlider.SetValue(RangeBase.ValueProperty, 4);

        }

        private void UpdateExample(SolidColorBrush newBrush)
        {
            if (ExampleEllipse != null)
            {
                ExampleEllipse.Width = ExampleEllipse.Height = GlobalInkSettings.Size;
                ExampleEllipse.Fill = newBrush;
            }
            if (GlobalInkSettings.StrokeType != GlobalInkSettings.StrokeTypes.Pencil && Column3.Width.Value == 5)
            {
                Column3.Width = new GridLength(0);
                Column4.Width = new GridLength(0);
            }
            else if (GlobalInkSettings.StrokeType == GlobalInkSettings.StrokeTypes.Pencil && Column3.Width.Value == 0)
            {
                Column3.Width = new GridLength(5);
                Column4.Width = new GridLength(50);
            }
        }

        private void OpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            GlobalInkSettings.Opacity = OpacitySlider.Value / 100;
            GlobalInkSettings.UpdateInkPresenters();
        }

        private void SizeSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            GlobalInkSettings.Size = SizeSlider.Value;
            GlobalInkSettings.UpdateInkPresenters();

        }

        private void BrightnessSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            GlobalInkSettings.Brightness = BrightnessSlider.Value;
            GlobalInkSettings.UpdateInkPresenters();

        }
    }
}
