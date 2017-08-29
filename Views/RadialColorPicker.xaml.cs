using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RadialColorPicker : UserControl
    {
        private Point _trackerPoint = new Point(0,0);
        public RadialColorPicker()
        {
            this.InitializeComponent();
            Loaded += RadialColorPicker_Loaded;
        }

        private void RadialColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            _trackerPoint = Util.PointTransformFromVisual(new Point(5, 15), RotationGrid, ColorEllipse);
            GlobalInkSettings.OnAttributesUpdated += GlobalInkSettingsOnOnAttributesUpdated;
            SetRotationFromPoint(_trackerPoint);
        }

        private void GlobalInkSettingsOnOnAttributesUpdated(SolidColorBrush newAttributes)
        {
            var newRotation = GlobalInkSettings.H;
            CompositeTransform newTransform = new CompositeTransform { Rotation = newRotation };
            RotationGrid.RenderTransform = newTransform;
        }

        private void IndicatorRectangle_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            _trackerPoint.X += e.Delta.Translation.X;
            _trackerPoint.Y += e.Delta.Translation.Y;
            SetRotationFromPoint(_trackerPoint);
            e.Handled = true;
        }

        private void IndicatorRectangle_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _trackerPoint = e.Position;
        }

        private double GetAngleOfPoint(Point point, FrameworkElement relativeTo)
        {
            //Translates and reflects point
            Point tranformedPoint = new Point(point.X - relativeTo.ActualWidth / 2, -point.Y + relativeTo.ActualHeight / 2);
            double angle = -Math.Atan(tranformedPoint.Y / tranformedPoint.X) * 180 / Math.PI + 90;
            if (tranformedPoint.X < 0) angle += 180;
            return angle;
        }

        private void ColorEllipse_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Point pos = e.GetPosition(ColorEllipse);
            SetRotationFromPoint(pos);
            e.Handled = true;
        }

        private void SetRotationFromPoint(Point pos)
        {
            double newRotation = GetAngleOfPoint(pos, ColorEllipse);
            GlobalInkSettings.H = newRotation;
        }
    }
}
