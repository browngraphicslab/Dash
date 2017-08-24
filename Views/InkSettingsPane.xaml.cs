using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InkSettingsPane : UserControl
    {
        public Symbol BrightnessSymbol { get; set; } = (Symbol) 0xE706;
        public Symbol SizeSymbol { get; set; } = (Symbol)0xEDA8;
        public Symbol OpacitySymbol { get; set; } = (Symbol) 0xEB42;

        private InkStroke ExampleStroke;

        public InkSettingsPane()
        {
            this.InitializeComponent();
            GlobalInkSettings.OnAttributesUpdated += (newBrush) => UpdateExample(newBrush);
            SizeSlider.Loaded += SizeSliderOnLoaded;
            Loaded += InkSettingsPane_Loaded;
            
        }

        private void InkSettingsPane_Loaded(object sender, RoutedEventArgs e)
        {
            var builder = new InkStrokeBuilder();
            var stroke = builder.CreateStrokeFromInkPoints(_strokeBuilderList, new Matrix3x2(1, 0, 0, 1, 0, 0));
            var point1 = stroke.GetInkPoints()[0].Position;
            //XInkCanvas.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());
            //XInkCanvas.InkPresenter.StrokeContainer.GetStrokes()[0].Selected = true;
            //XInkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(30 - point1.X, 45 - point1.Y));
            //XInkCanvas.InkPresenter.IsInputEnabled = false;
            //ExampleStroke = XInkCanvas.InkPresenter.StrokeContainer.GetStrokes()[0];
        }

        private void SizeSliderOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SizeSlider.SetValue(RadialMenuSlider.RelativeValueProperty, 0.5);
        }

        private void UpdateExample(SolidColorBrush newBrush)
        {
            if (ExampleStroke != null)
            {
                ExampleStroke.DrawingAttributes = GlobalInkSettings.Attributes;
            }
            if (GlobalInkSettings.StrokeType == GlobalInkSettings.StrokeTypes.Eraser)
            {
                Visibility = Visibility.Collapsed;
            }
            else
            {
                Visibility = Visibility.Visible;
            }
            //}
            //if (GlobalInkSettings.StrokeType != GlobalInkSettings.StrokeTypes.Pencil && Column3.Width.RelativeValue != 0)
            //{
            //    Column3.Width = new GridLength(0);
            //    Column4.Width = new GridLength(0);
            //}
            //else if (GlobalInkSettings.StrokeType == GlobalInkSettings.StrokeTypes.Pencil && Column3.Width.RelativeValue == 0)
            //{
            //    Column3.Width = new GridLength(5);
            //    Column4.Width = new GridLength(30);
            //}
        }
        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            GlobalInkSettings.Color = Color.FromArgb(255, 128, 128, 128);
            GlobalInkSettings.UpdateInkPresenters();
        }
        

        private void OpacitySlider_OnValueChanged(RadialMenuSlider sender, double newvalue)
        {
            GlobalInkSettings.Opacity = newvalue/100;
            GlobalInkSettings.UpdateInkPresenters();
        }

        private void SizeSlider_OnValueChanged(RadialMenuSlider sender, double newvalue)
        {
            GlobalInkSettings.Size = newvalue;
            GlobalInkSettings.UpdateInkPresenters();
        }

        private void BrightnessSlider_OnValueChanged(RadialMenuSlider sender, double newvalue)
        {
            GlobalInkSettings.Brightness = newvalue;
            GlobalInkSettings.UpdateInkPresenters();
        }

        private List<InkPoint> _strokeBuilderList = new List<InkPoint>
        {
            new InkPoint (new Point(30430.93359375, 30556.953125), (float)0.1015625, (float)0, (float)0, (ulong)587748170684),
            new InkPoint (new Point(30428.41015625, 30557.63671875), (float)0.1416016, (float)0, (float)0, (ulong)587748170750),
            new InkPoint (new Point(30427.9375, 30558.05859375), (float)0.1601563, (float)0, (float)0, (ulong)587748170757),
            new InkPoint (new Point(30427.462890625, 30558.478515625), (float)0.1699219, (float)0, (float)0, (ulong)587748179217),
            new InkPoint (new Point(30427.462890625, 30558.478515625), (float)0.1933594, (float)0, (float)0, (ulong)587748185748),
            new InkPoint (new Point(30427.462890625, 30558.478515625), (float)0.2080078, (float)0, (float)0, (ulong)587748194284),
            new InkPoint (new Point(30427.462890625, 30558.3203125), (float)0.2197266, (float)0, (float)0, (ulong)587748200847),
            new InkPoint (new Point(30427.779296875, 30557.900390625), (float)0.2285156, (float)0, (float)0, (ulong)587748209295),
            new InkPoint (new Point(30428.41015625, 30557.48046875), (float)0.2382813, (float)0, (float)0, (ulong)587748215858),
            new InkPoint (new Point(30429.35546875, 30556.796875), (float)0.2431641, (float)0, (float)0, (ulong)587748224396),
            new InkPoint (new Point(30430.302734375, 30556.26953125), (float)0.2539063, (float)0, (float)0, (ulong)587748224463),
            new InkPoint (new Point(30431.197265625, 30555.53515625), (float)0.2685547, (float)0, (float)0, (ulong)587748230716),
            new InkPoint (new Point(30431.984375, 30555.0078125), (float)0.2783203, (float)0, (float)0, (ulong)587748239342),
            new InkPoint (new Point(30432.7734375, 30554.4296875), (float)0.2851563, (float)0, (float)0, (ulong)587748245918),
            new InkPoint (new Point(30433.5625, 30554.16796875), (float)0.2900391, (float)0, (float)0, (ulong)587748254362),
            new InkPoint (new Point(30434.193359375, 30553.904296875), (float)0.2958984, (float)0, (float)0, (ulong)587748260944),
            new InkPoint (new Point(30434.666015625, 30553.58984375), (float)0.3007813, (float)0, (float)0, (ulong)587748269301),
            new InkPoint (new Point(30435.296875, 30553.220703125), (float)0.3134766, (float)0, (float)0, (ulong)587748284361),
            new InkPoint (new Point(30435.927734375, 30552.80078125), (float)0.3300781, (float)0, (float)0, (ulong)587748290929),
            new InkPoint (new Point(30436.6640625, 30552.22265625), (float)0.3427734, (float)0, (float)0, (ulong)587748299361),
            new InkPoint (new Point(30437.453125, 30551.5390625), (float)0.3525391, (float)0, (float)0, (ulong)587748305928),
            new InkPoint (new Point(30438.2421875, 30550.85546875), (float)0.359375, (float)0, (float)0, (ulong)587748314337),
            new InkPoint (new Point(30439.03125, 30550.013671875), (float)0.3662109, (float)0, (float)0, (ulong)587748320887),
            new InkPoint (new Point(30440.134765625, 30549.171875), (float)0.3691406, (float)0, (float)0, (ulong)587748329390),
            new InkPoint (new Point(30441.080078125, 30548.33203125), (float)0.3720703, (float)0, (float)0, (ulong)587748335912),
            new InkPoint (new Point(30442.185546875, 30547.0703125), (float)0.3740234, (float)0, (float)0, (ulong)587748344401),
            new InkPoint (new Point(30443.236328125, 30545.859375), (float)0.3740234, (float)0, (float)0, (ulong)587748350986),
            new InkPoint (new Point(30444.498046875, 30544.44140625), (float)0.3720703, (float)0, (float)0, (ulong)587748359641),
            new InkPoint (new Point(30445.759765625, 30542.7578125), (float)0.3710938, (float)0, (float)0, (ulong)587748366134),
            new InkPoint (new Point(30447.49609375, 30540.970703125), (float)0.3710938, (float)0, (float)0, (ulong)587748374589),
            new InkPoint (new Point(30449.3359375, 30539.025390625), (float)0.3710938, (float)0, (float)0, (ulong)587748381007),
            new InkPoint (new Point(30451.0703125, 30537.080078125), (float)0.3710938, (float)0, (float)0, (ulong)587748389476),
            new InkPoint (new Point(30452.8046875, 30535.134765625), (float)0.3730469, (float)0, (float)0, (ulong)587748396132),
            new InkPoint (new Point(30454.171875, 30533.453125), (float)0.3740234, (float)0, (float)0, (ulong)587748404604),
            new InkPoint (new Point(30455.43359375, 30532.0859375), (float)0.3769531, (float)0, (float)0, (ulong)587748411179),
            new InkPoint (new Point(30456.380859375, 30530.98046875), (float)0.3779297, (float)0, (float)0, (ulong)587748419706),
            new InkPoint (new Point(30457.169921875, 30530.140625), (float)0.3779297, (float)0, (float)0, (ulong)587748426254),
            new InkPoint (new Point(30457.642578125, 30529.45703125), (float)0.3916016, (float)0, (float)0, (ulong)587748434669),
            new InkPoint (new Point(30458.115234375, 30529.03515625), (float)0.4101563, (float)0, (float)0, (ulong)587748441230),
            new InkPoint (new Point(30458.2734375, 30528.7734375), (float)0.4277344, (float)0, (float)0, (ulong)587748449599),
            new InkPoint (new Point(30458.2734375, 30528.7734375), (float)0.4453125, (float)0, (float)0, (ulong)587748456323),
            new InkPoint (new Point(30458.115234375, 30529.03515625), (float)0.4550781, (float)0, (float)0, (ulong)587748464555),
            new InkPoint (new Point(30457.80078125, 30529.45703125), (float)0.4609375, (float)0, (float)0, (ulong)587748471373),
            new InkPoint (new Point(30457.169921875, 30530.296875), (float)0.4609375, (float)0, (float)0, (ulong)587748479614),
            new InkPoint (new Point(30456.5390625, 30531.138671875), (float)0.4609375, (float)0, (float)0, (ulong)587748486344),
            new InkPoint (new Point(30455.591796875, 30532.34765625), (float)0.4589844, (float)0, (float)0, (ulong)587748494676),
            new InkPoint (new Point(30454.330078125, 30534.03125), (float)0.4570313, (float)0, (float)0, (ulong)587748501268),
            new InkPoint (new Point(30453.2265625, 30535.712890625), (float)0.4550781, (float)0, (float)0, (ulong)587748509673),
            new InkPoint (new Point(30452.017578125, 30537.921875), (float)0.453125, (float)0, (float)0, (ulong)587748516431),
            new InkPoint (new Point(30450.755859375, 30540.12890625), (float)0.453125, (float)0, (float)0, (ulong)587748524692),
            new InkPoint (new Point(30449.650390625, 30542.390625), (float)0.453125, (float)0, (float)0, (ulong)587748531174),
            new InkPoint (new Point(30448.546875, 30545.01953125), (float)0.453125, (float)0, (float)0, (ulong)587748539730),
            new InkPoint (new Point(30447.65234375, 30547.384765625), (float)0.453125, (float)0, (float)0, (ulong)587748546296),
            new InkPoint (new Point(30446.70703125, 30549.59375), (float)0.453125, (float)0, (float)0, (ulong)587748554657),
            new InkPoint (new Point(30445.91796875, 30551.380859375), (float)0.4550781, (float)0, (float)0, (ulong)587748561188),
            new InkPoint (new Point(30445.4453125, 30552.90625), (float)0.4560547, (float)0, (float)0, (ulong)587748569745),
            new InkPoint (new Point(30445.12890625, 30554.16796875), (float)0.4560547, (float)0, (float)0, (ulong)587748576222),
            new InkPoint (new Point(30444.970703125, 30555.0078125), (float)0.4541016, (float)0, (float)0, (ulong)587748584760),
            new InkPoint (new Point(30444.970703125, 30555.69140625), (float)0.453125, (float)0, (float)0, (ulong)587748591264),
            new InkPoint (new Point(30445.287109375, 30555.955078125), (float)0.453125, (float)0, (float)0, (ulong)587748599674),
            new InkPoint (new Point(30445.759765625, 30555.955078125), (float)0.4550781, (float)0, (float)0, (ulong)587748606335),
            new InkPoint (new Point(30446.548828125, 30555.849609375), (float)0.4580078, (float)0, (float)0, (ulong)587748614699),
            new InkPoint (new Point(30447.65234375, 30555.166015625), (float)0.4619141, (float)0, (float)0, (ulong)587748621364),
            new InkPoint (new Point(30449.01953125, 30554.4296875), (float)0.4609375, (float)0, (float)0, (ulong)587748629728),
            new InkPoint (new Point(30450.28125, 30553.74609375), (float)0.4609375, (float)0, (float)0, (ulong)587748636373),
            new InkPoint (new Point(30451.38671875, 30553.0625), (float)0.4609375, (float)0, (float)0, (ulong)587748644918),
            new InkPoint (new Point(30452.490234375, 30552.484375), (float)0.4609375, (float)0, (float)0, (ulong)587748651261),
            new InkPoint (new Point(30453.541015625, 30551.958984375), (float)0.4609375, (float)0, (float)0, (ulong)587748659912),
            new InkPoint (new Point(30454.48828125, 30551.5390625), (float)0.4609375, (float)0, (float)0, (ulong)587748666505),
            new InkPoint (new Point(30455.27734375, 30551.275390625), (float)0.4580078, (float)0, (float)0, (ulong)587748674838),
            new InkPoint (new Point(30456.064453125, 30551.1171875), (float)0.4570313, (float)0, (float)0, (ulong)587748681221),
            new InkPoint (new Point(30456.853515625, 30550.9609375), (float)0.4570313, (float)0, (float)0, (ulong)587748689826),
            new InkPoint (new Point(30457.642578125, 30550.9609375), (float)0.4589844, (float)0, (float)0, (ulong)587748704941),
            new InkPoint (new Point(30458.2734375, 30551.1171875), (float)0.4599609, (float)0, (float)0, (ulong)587748711529),
            new InkPoint (new Point(30458.8515625, 30551.380859375), (float)0.4599609, (float)0, (float)0, (ulong)587748719956),
            new InkPoint (new Point(30459.32421875, 30551.64453125), (float)0.4609375, (float)0, (float)0, (ulong)587748726551),
            new InkPoint (new Point(30459.798828125, 30552.22265625), (float)0.4609375, (float)0, (float)0, (ulong)587748735017),
            new InkPoint (new Point(30460.4296875, 30552.642578125), (float)0.4628906, (float)0, (float)0, (ulong)587748741415),
            new InkPoint (new Point(30460.90234375, 30553.0625), (float)0.4638672, (float)0, (float)0, (ulong)587748749827),
            new InkPoint (new Point(30461.69140625, 30553.326171875), (float)0.4638672, (float)0, (float)0, (ulong)587748756393),
            new InkPoint (new Point(30462.48046875, 30553.326171875), (float)0.4667969, (float)0, (float)0, (ulong)587748764801),
            new InkPoint (new Point(30463.42578125, 30553.0625), (float)0.4716797, (float)0, (float)0, (ulong)587748771379),
            new InkPoint (new Point(30464.4765625, 30552.642578125), (float)0.4746094, (float)0, (float)0, (ulong)587748779805),
            new InkPoint (new Point(30465.58203125, 30552.064453125), (float)0.4755859, (float)0, (float)0, (ulong)587748786405),
            new InkPoint (new Point(30466.685546875, 30551.380859375), (float)0.4785156, (float)0, (float)0, (ulong)587748794894),
            new InkPoint (new Point(30468.10546875, 30550.5390625), (float)0.4833984, (float)0, (float)0, (ulong)587748801444),
            new InkPoint (new Point(30469.62890625, 30549.69921875), (float)0.484375, (float)0, (float)0, (ulong)587748809966),
            new InkPoint (new Point(30471.20703125, 30549.015625), (float)0.4824219, (float)0, (float)0, (ulong)587748816565),
            new InkPoint (new Point(30472.626953125, 30548.48828125), (float)0.4814453, (float)0, (float)0, (ulong)587748825016),
            new InkPoint (new Point(30473.888671875, 30548.068359375), (float)0.4804688, (float)0, (float)0, (ulong)587748831526),
            new InkPoint (new Point(30474.9921875, 30547.6484375), (float)0.4824219, (float)0, (float)0, (ulong)587748839994),
            new InkPoint (new Point(30475.88671875, 30547.384765625), (float)0.4873047, (float)0, (float)0, (ulong)587748846535),
            new InkPoint (new Point(30476.83203125, 30547.2265625), (float)0.4882813, (float)0, (float)0, (ulong)587748854974),
            new InkPoint (new Point(30477.779296875, 30547.2265625), (float)0.4882813, (float)0, (float)0, (ulong)587748861640),
            new InkPoint (new Point(30478.724609375, 30547.384765625), (float)0.4882813, (float)0, (float)0, (ulong)587748870130),
            new InkPoint (new Point(30479.830078125, 30547.6484375), (float)0.4882813, (float)0, (float)0, (ulong)587748876744),
            new InkPoint (new Point(30480.880859375, 30547.91015625), (float)0.4882813, (float)0, (float)0, (ulong)587748885122),
            new InkPoint (new Point(30482.142578125, 30548.173828125), (float)0.4882813, (float)0, (float)0, (ulong)587748891690),
            new InkPoint (new Point(30483.404296875, 30548.59375), (float)0.4853516, (float)0, (float)0, (ulong)587748899999),
            new InkPoint (new Point(30484.82421875, 30549.015625), (float)0.4824219, (float)0, (float)0, (ulong)587748906774),
            new InkPoint (new Point(30486.19140625, 30549.435546875), (float)0.4746094, (float)0, (float)0, (ulong)587748915003),
            new InkPoint (new Point(30487.92578125, 30549.85546875), (float)0.4667969, (float)0, (float)0, (ulong)587748921774),
            new InkPoint (new Point(30489.9765625, 30550.119140625), (float)0.4394531, (float)0, (float)0, (ulong)587748930028),
            new InkPoint (new Point(30491.974609375, 30550.27734375), (float)0.4033203, (float)0, (float)0, (ulong)587748936570),
            new InkPoint (new Point(30493.8671875, 30550.013671875), (float)0.2822266, (float)0, (float)0, (ulong)587748951792),
        };

       
    }
}
