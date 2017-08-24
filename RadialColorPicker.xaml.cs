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
            SetRotationFromPoint(_trackerPoint);
        }

        private void UpdateColor()
        {
            var rotation = (RotationGrid.RenderTransform as CompositeTransform).Rotation;
            
            Color color = HsvToRgb(rotation, 1, 1);
            GlobalInkSettings.Color = color;
            GlobalInkSettings.UpdateInkPresenters();
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
            CompositeTransform newTransform = new CompositeTransform { Rotation = newRotation };
            RotationGrid.RenderTransform = newTransform;
            UpdateColor();
        }

        private Color HsvToRgb(double h, double S, double V)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            var r = Clamp((int)(R * 255.0));
            var g = Clamp((int)(G * 255.0));
            var b = Clamp((int)(B * 255.0));
            return Color.FromArgb(255, (byte) r, (byte) g, (byte) b);
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
