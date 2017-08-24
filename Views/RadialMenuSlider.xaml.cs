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
    public sealed partial class RadialMenuSlider : UserControl
    {
        private double MinAngle;
        private double MaxAngle;
        public double AngleBuffer = 1.5;

        public static readonly DependencyProperty InnerRadiusProperty =
            DependencyProperty.Register("InnerRadius", typeof(double), typeof(RadialMenuSlider), null);
        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register("Thickness", typeof(double), typeof(RadialMenuSlider), null);
        public static readonly DependencyProperty ArcRadiusProperty =
            DependencyProperty.Register("ArcRadius", typeof(Size), typeof(RadialMenuSlider), null);
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(RadialMenuSlider), null);
        public static readonly DependencyProperty SliderAngleProperty =
            DependencyProperty.Register("SliderAngle", typeof(double), typeof(RadialMenuSlider), null);
        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(RadialMenuSlider), null);
        public static readonly DependencyProperty RelativeValueProperty =
            DependencyProperty.Register("RelativeValue", typeof(double), typeof(RadialMenuSlider), null);
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(RadialMenuSlider), null);

        private Point _trackerPoint;
        private double _value;
        private Symbol? _symbol;
        private SymbolIcon _icon;


        public double InnerRadius
        {
            get { return(double) GetValue(InnerRadiusProperty); }
            set { SetValue(InnerRadiusProperty, value); }
        }

        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty);}
            set { SetValue(ThicknessProperty, value); }
        }

        public Size ArcRadius
        {
            get { return (Size)GetValue(ArcRadiusProperty); }
            set { SetValue(ArcRadiusProperty, value); }
        }

        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public double SliderAngle {
            get { return (double)GetValue(SliderAngleProperty); }
            set { SetValue(SliderAngleProperty, value); }
        }

        public double MinValue { get; set; } = 0;
        public double MaxValue { get; set; } = 1;

        public double RelativeValue
        {
            get { return (double)GetValue(RelativeValueProperty); }
            set
            {
                SetValue(RelativeValueProperty, value);
                RotationGrid.RenderTransform = new CompositeTransform { Rotation = value * (MaxAngle - MinAngle) + MinAngle };
                Value = value * (MaxValue - MinValue) + MinValue;
            }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set
            {
                SetValue(ValueProperty, value);
                ValueChanged?.Invoke(this, value);
            }
        }

        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        public delegate void RadialSliderValueChangedEventHandler(RadialMenuSlider sender, double newValue);

        public event RadialSliderValueChangedEventHandler ValueChanged;


        public Point EndPoint { get; set; }
        public Point StartPoint { get; set; }

        public Symbol? Symbol
        {
            get { return _symbol; }
            set
            {
                _symbol = value;
                Draw();
            }
        }


        public RadialMenuSlider()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            MinAngle = StartAngle + 5 + AngleBuffer;
            MaxAngle = StartAngle + Angle - AngleBuffer;
            RelativeValue = 0.5;
            Draw();
        }

        private void Draw()
        {
            ArcRadius = new Size(InnerRadius + Thickness / 2, InnerRadius + Thickness / 2);
            var theta1 = (StartAngle + 5) * Math.PI / 360;
            var InnerStartPoint = new Point(ArcRadius.Width * 2 * Math.Cos(theta1) * Math.Sin(theta1), ArcRadius.Height * Math.Sin(theta1) * Math.Sin(theta1) * 2);
            var theta2 = (Angle + StartAngle) * Math.PI / 360;
            EndPoint = new Point(ArcRadius.Width * 2 * Math.Cos(theta2) * Math.Sin(theta2), ArcRadius.Height * Math.Sin(theta2) * Math.Sin(theta2) * 2);
            var theta3 = StartAngle * Math.PI / 360;
            StartPoint = new Point(ArcRadius.Width * 2 * Math.Cos(theta3) * Math.Sin(theta3), ArcRadius.Height * Math.Sin(theta3) * Math.Sin(theta3) * 2);
            if (Symbol != null)
            {
                Canvas.Children.Remove(_icon);
                _icon = new SymbolIcon((Symbol)Symbol);
                Canvas.Children.Add(_icon);
                _icon.Foreground = new SolidColorBrush(Colors.White);
                _icon.IsHitTestVisible = false;
                _icon.RenderTransform = new ScaleTransform()
                {
                    CenterX = 0,
                    CenterY = 0,
                    ScaleX = 0.7,
                    ScaleY = 0.7
                };
                _icon.Loaded += (sender, args) =>
                {
                    Canvas.SetLeft(_icon, StartPoint.X - _icon.ActualWidth/3 - 6);
                    Canvas.SetTop(_icon, StartPoint.Y - _icon.ActualHeight/3 - 7);
                };
            }
            else
            {
                InnerStartPoint = StartPoint;
            }
            OuterPath.StrokeThickness = Thickness;
            OuterPathArcSegment.Size = ArcRadius;
            OuterPathArcSegment.Point = EndPoint;
            OuterPathFigure.StartPoint = StartPoint;
            SliderPathArcSegment.Size = ArcRadius;
            SliderPathArcSegment.Point = EndPoint;
            SliderPathFigure.StartPoint = InnerStartPoint;
            RotationGrid.Margin = new Thickness(-RotationGrid.Width / 2, -Thickness / 2, 0, 0);
            RotationGrid.Height = InnerRadius + Thickness;
            ReferencePoint.Margin = new Thickness(-RotationGrid.Width / 2, RotationGrid.Height - Thickness / 2, 0, 0);
            DraggerEllipse.Margin = new Thickness(0, Thickness/2 - 5, 0,0);
            RelativeValue = RelativeValue;
        }

        private void Dragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            _trackerPoint.X += e.Delta.Translation.X;
            _trackerPoint.Y += e.Delta.Translation.Y;
            SetRotationFromPoint(_trackerPoint);
            Point draggerPoint = Util.PointTransformFromVisual(new Point(5, 5), DraggerEllipse, Canvas);
            Canvas.SetLeft(Label, draggerPoint.X + 10);
            Canvas.SetTop(Label, draggerPoint.Y);
            Label.Text = ((int) Value).ToString();
            e.Handled = true;
        }

        private void Dragger_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _trackerPoint = Util.PointTransformFromVisual(e.Position, sender as UIElement, ReferencePoint);
            Label.Visibility = Visibility.Visible;
        }

        private double GetAngleOfPoint(Point point)
        {
            //Translates and reflects point
            double angle = 90 - Math.Atan(-point.Y / point.X) * 180 / Math.PI;
            return angle;
        }

        private void SetRotationFromPoint(Point pos)
        {
            double newRotation = GetAngleOfPoint(pos);
            if (newRotation < MinAngle) newRotation = MinAngle;
            if (newRotation > MaxAngle) newRotation = MaxAngle;
            RelativeValue = (newRotation - MinAngle) / (MaxAngle - MinAngle);
        }

        private void OuterPath_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Label.Visibility = Visibility.Collapsed;
        }

        private void OuterPath_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _trackerPoint = Util.PointTransformFromVisual(e.GetPosition(sender as UIElement), sender as UIElement, ReferencePoint);
            SetRotationFromPoint(_trackerPoint);
            e.Handled = true;
        }
    }
}
