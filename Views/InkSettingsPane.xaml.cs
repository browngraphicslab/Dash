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
            XInkCanvas.InkPresenter.StrokeContainer.AddStroke(stroke.Clone());
            XInkCanvas.InkPresenter.StrokeContainer.GetStrokes()[0].Selected = true;
            XInkCanvas.InkPresenter.StrokeContainer.MoveSelected(new Point(5, 13));
            XInkCanvas.InkPresenter.IsInputEnabled = false;
            ExampleStroke = XInkCanvas.InkPresenter.StrokeContainer.GetStrokes()[0];
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
        }

        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs e)
        {
            Debug.WriteLine("new stroke ======");
            foreach (var point in e.Strokes[0].GetInkPoints())
            {
                Debug.WriteLine("new InkPoint( new Point(" + point.Position + "), (float)" + point.Pressure + ", (float)" + point.TiltX + ", (float)" + point.TiltY + ", (ulong)" + point.Timestamp + "),");
            }
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
            if (GlobalInkSettings.StrokeType != GlobalInkSettings.StrokeTypes.Pencil && OpacitySlider.Visibility == Visibility.Visible)
            {
                OpacitySlider.Visibility = Visibility.Collapsed;
            }
            else if (GlobalInkSettings.StrokeType == GlobalInkSettings.StrokeTypes.Pencil && OpacitySlider.Visibility == Visibility.Collapsed)
            {
                OpacitySlider.Visibility = Visibility.Visible;
            }
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
            new InkPoint( new Point(57.71411,7.072601), (float)0.0703125, (float)0, (float)0, (ulong)1015062801132),
            new InkPoint( new Point(57.71411,6.651978), (float)0.09960938, (float)0, (float)0, (ulong)1015062801266),
            new InkPoint( new Point(57.71411,6.651978), (float)0.1132813, (float)0, (float)0, (ulong)1015062801285),
            new InkPoint( new Point(57.71411,6.546814), (float)0.1367188, (float)0, (float)0, (ulong)1015062809235),
            new InkPoint( new Point(57.71411,6.546814), (float)0.1757813, (float)0, (float)0, (ulong)1015062816002),
            new InkPoint( new Point(57.71411,6.546814), (float)0.2167969, (float)0, (float)0, (ulong)1015062824433),
            new InkPoint( new Point(57.71411,6.546814), (float)0.2587891, (float)0, (float)0, (ulong)1015062831035),
            new InkPoint( new Point(57.71411,6.546814), (float)0.2890625, (float)0, (float)0, (ulong)1015062839720),
            new InkPoint( new Point(57.71411,6.546814), (float)0.3154297, (float)0, (float)0, (ulong)1015062846111),
            new InkPoint( new Point(57.71411,6.546814), (float)0.3261719, (float)0, (float)0, (ulong)1015062854752),
            new InkPoint( new Point(57.71411,6.546814), (float)0.3408203, (float)0, (float)0, (ulong)1015062854835),
            new InkPoint( new Point(57.71411,6.546814), (float)0.3583984, (float)0, (float)0, (ulong)1015062861208),
            new InkPoint( new Point(57.71411,6.546814), (float)0.3789063, (float)0, (float)0, (ulong)1015062869537),
            new InkPoint( new Point(57.71411,6.546814), (float)0.4013672, (float)0, (float)0, (ulong)1015062876162),
            new InkPoint( new Point(57.71411,6.546814), (float)0.4228516, (float)0, (float)0, (ulong)1015062884428),
            new InkPoint( new Point(57.71411,6.546814), (float)0.4423828, (float)0, (float)0, (ulong)1015062901727),
            new InkPoint( new Point(57.71411,6.809692), (float)0.4599609, (float)0, (float)0, (ulong)1015062902058),
            new InkPoint( new Point(57.39868,7.072601), (float)0.4785156, (float)0, (float)0, (ulong)1015062906083),
            new InkPoint( new Point(57.39868,7.388031), (float)0.4931641, (float)0, (float)0, (ulong)1015062914641),
            new InkPoint( new Point(57.39868,7.388031), (float)0.5087891, (float)0, (float)0, (ulong)1015062921233),
            new InkPoint( new Point(57.24097,7.65094), (float)0.5283203, (float)0, (float)0, (ulong)1015062929808),
            new InkPoint( new Point(57.24097,7.808655), (float)0.5498047, (float)0, (float)0, (ulong)1015062936176),
            new InkPoint( new Point(57.24097,7.808655), (float)0.5712891, (float)0, (float)0, (ulong)1015062944764),
            new InkPoint( new Point(57.24097,7.913818), (float)0.59375, (float)0, (float)0, (ulong)1015062951147),
            new InkPoint( new Point(57.24097,7.913818), (float)0.6142578, (float)0, (float)0, (ulong)1015062959762),
            new InkPoint( new Point(57.24097,8.229279), (float)0.6367188, (float)0, (float)0, (ulong)1015062966193),
            new InkPoint( new Point(56.92548,8.334412), (float)0.6572266, (float)0, (float)0, (ulong)1015062974861),
            new InkPoint( new Point(56.92548,8.334412), (float)0.6767578, (float)0, (float)0, (ulong)1015062981240),
            new InkPoint( new Point(56.76776,8.492157), (float)0.6972656, (float)0, (float)0, (ulong)1015062989740),
            new InkPoint( new Point(56.76776,8.492157), (float)0.7167969, (float)0, (float)0, (ulong)1015062996230),
            new InkPoint( new Point(56.61005,8.59729), (float)0.7314453, (float)0, (float)0, (ulong)1015063004826),
            new InkPoint( new Point(56.61005,8.59729), (float)0.7470703, (float)0, (float)0, (ulong)1015063011500),
            new InkPoint( new Point(56.45233,8.755035), (float)0.7578125, (float)0, (float)0, (ulong)1015063019851),
            new InkPoint( new Point(56.29456,8.91275), (float)0.7675781, (float)0, (float)0, (ulong)1015063026460),
            new InkPoint( new Point(56.13684,9.017914), (float)0.7773438, (float)0, (float)0, (ulong)1015063034671),
            new InkPoint( new Point(56.13684,9.175629), (float)0.7910156, (float)0, (float)0, (ulong)1015063041374),
            new InkPoint( new Point(56.13684,9.175629), (float)0.8007813, (float)0, (float)0, (ulong)1015063049354),
            new InkPoint( new Point(56.13684,9.175629), (float)0.8105469, (float)0, (float)0, (ulong)1015063056445),
            new InkPoint( new Point(55.97913,9.438507), (float)0.8183594, (float)0, (float)0, (ulong)1015063064740),
            new InkPoint( new Point(55.82141,9.753967), (float)0.8261719, (float)0, (float)0, (ulong)1015063071320),
            new InkPoint( new Point(55.82141,9.753967), (float)0.8359375, (float)0, (float)0, (ulong)1015063079815),
            new InkPoint( new Point(55.66364,9.859131), (float)0.8466797, (float)0, (float)0, (ulong)1015063086200),
            new InkPoint( new Point(55.55853,10.01685), (float)0.8564453, (float)0, (float)0, (ulong)1015063094752),
            new InkPoint( new Point(55.24304,10.17459), (float)0.8671875, (float)0, (float)0, (ulong)1015063101200),
            new InkPoint( new Point(54.7699,10.27972), (float)0.8720703, (float)0, (float)0, (ulong)1015063109940),
            new InkPoint( new Point(54.13898,10.54263), (float)0.8740234, (float)0, (float)0, (ulong)1015063116353),
            new InkPoint( new Point(53.50806,10.96323), (float)0.875, (float)0, (float)0, (ulong)1015063124957),
            new InkPoint( new Point(52.71942,11.27869), (float)0.8769531, (float)0, (float)0, (ulong)1015063131569),
            new InkPoint( new Point(51.93073,11.69931), (float)0.8798828, (float)0, (float)0, (ulong)1015063139910),
            new InkPoint( new Point(51.14209,12.06732), (float)0.8837891, (float)0, (float)0, (ulong)1015063146581),
            new InkPoint( new Point(50.35345,12.48795), (float)0.8876953, (float)0, (float)0, (ulong)1015063154804),
            new InkPoint( new Point(49.61737,12.90854), (float)0.8916016, (float)0, (float)0, (ulong)1015063161530),
            new InkPoint( new Point(48.51331,13.224), (float)0.8935547, (float)0, (float)0, (ulong)1015063170021),
            new InkPoint( new Point(47.25146,13.74976), (float)0.8964844, (float)0, (float)0, (ulong)1015063176591),
            new InkPoint( new Point(45.98962,14.43326), (float)0.8974609, (float)0, (float)0, (ulong)1015063184668),
            new InkPoint( new Point(44.72778,15.16934), (float)0.9003906, (float)0, (float)0, (ulong)1015063191377),
            new InkPoint( new Point(43.20313,15.95798), (float)0.9013672, (float)0, (float)0, (ulong)1015063199808),
            new InkPoint( new Point(41.46808,17.11465), (float)0.9013672, (float)0, (float)0, (ulong)1015063206624),
            new InkPoint( new Point(39.57532,18.32391), (float)0.9023438, (float)0, (float)0, (ulong)1015063215053),
            new InkPoint( new Point(37.57745,19.8486), (float)0.9023438, (float)0, (float)0, (ulong)1015063221531),
            new InkPoint( new Point(35.36926,21.53107), (float)0.8994141, (float)0, (float)0, (ulong)1015063230047),
            new InkPoint( new Point(33.21362,23.2135), (float)0.8964844, (float)0, (float)0, (ulong)1015063236563),
            new InkPoint( new Point(30.68994,25.15881), (float)0.8925781, (float)0, (float)0, (ulong)1015063244844),
            new InkPoint( new Point(28.32404,27.20929), (float)0.8916016, (float)0, (float)0, (ulong)1015063251257),
            new InkPoint( new Point(26.01068,29.57523), (float)0.8837891, (float)0, (float)0, (ulong)1015063260115),
            new InkPoint( new Point(23.80249,31.836), (float)0.8769531, (float)0, (float)0, (ulong)1015063266454),
            new InkPoint( new Point(21.48914,34.14935), (float)0.8496094, (float)0, (float)0, (ulong)1015063274931),
            new InkPoint( new Point(19.12317,36.673), (float)0.8134766, (float)0, (float)0, (ulong)1015063281474),
            new InkPoint( new Point(16.80981,39.5647), (float)0.7529297, (float)0, (float)0, (ulong)1015063289884),
            new InkPoint( new Point(14.44391,42.66669), (float)0.6816406, (float)0, (float)0, (ulong)1015063296450),
            new InkPoint( new Point(12.23572,45.979), (float)0.5947266, (float)0, (float)0, (ulong)1015063305007),
            new InkPoint( new Point(10.39551,49.29129), (float)0.5009766, (float)0, (float)0, (ulong)1015063311484),
            new InkPoint( new Point(8.345032,52.76132), (float)0.3798828, (float)0, (float)0, (ulong)1015063326715),
            new InkPoint( new Point(6.189453,56.96744), (float)0.2070313, (float)0, (float)0, (ulong)1015063335230),
            new InkPoint( new Point(4.296692,61.12097), (float)0.08691406, (float)0, (float)0, (ulong)1015063341877),
        };

       
    }
}
