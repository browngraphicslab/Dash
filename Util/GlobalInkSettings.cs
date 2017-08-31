using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public static class GlobalInkSettings
    {
        public delegate void BooleanToggledEventHandler(bool newValue);

        public delegate void InkInputChangedEventHandler(CoreInputDeviceTypes newInputType);

        public delegate void AttributesUpdatedEventHandler(SolidColorBrush newAttributes);

        public static event AttributesUpdatedEventHandler OnAttributesUpdated;

        public enum StrokeTypes
        {
            Pen,
            Pencil,
            Eraser,
            Selection
        }

        private static InkDrawingAttributes _attributes = new InkDrawingAttributes();
        private static CoreInputDeviceTypes _inkInputType;
        private static bool _isRecognitionEnabled;
        private static Color _color = Colors.DarkGray;
        private static double _hue;
        private static double _brightness;
        private static StrokeTypes _strokeType;
        private static double _size = 3;
        private static double _opacity = 1;

        public static StrokeTypes StrokeType
        {
            get => _strokeType;
            set
            {
                if (_strokeType == value) return;
                _strokeType = value;
                foreach (var ctrl in FreeformInkControls)
                {
                    ctrl.UpdateSelectionMode();
                }
                var attributes = new InkDrawingAttributes();
                switch (value)
                {
                    case StrokeTypes.Eraser:
                        foreach (var presenter in Presenters)
                        {
                            presenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
                        }
                        break;
                    case StrokeTypes.Pencil:
                        attributes = InkDrawingAttributes.CreateForPencil();
                        attributes.PencilProperties.Opacity = Opacity;
                        attributes.Color = Color;
                        attributes.Size = new Size(Size, Size);
                        Attributes = attributes;
                        OnAttributesUpdated?.Invoke(new SolidColorBrush(Color)
                        {
                            Opacity = Opacity
                        });
                        foreach (var presenter in Presenters)
                        {
                            presenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                            presenter.UpdateDefaultDrawingAttributes(Attributes);
                        }
                        break;
                    case StrokeTypes.Pen:
                        attributes.Color = Color;
                        attributes.Size = new Size(Size, Size);
                        Attributes = attributes;
                        OnAttributesUpdated?.Invoke(new SolidColorBrush(Color)
                        {
                            Opacity = Opacity
                        });
                        foreach (var presenter in Presenters)
                        {
                            presenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                            presenter.UpdateDefaultDrawingAttributes(Attributes);
                        }
                        break;
                    case StrokeTypes.Selection:
                        foreach (var presenter in Presenters)
                        {
                            presenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }

        }

        public static CoreInputDeviceTypes InkInputType
        {
            get => _inkInputType;
            set
            {
                _inkInputType = value;
                foreach (var inkPresenter in Presenters)
                    inkPresenter.InputDeviceTypes = value;
                foreach (var ctrls in FreeformInkControls)
                    ctrls.UpdateInputType();
               
            }
        }

        public static double Brightness
        {
            get { return _brightness; }
            set
            {
                _brightness = value;
                Color color = HsvToRgb(Hue, 1, 1);
                Color = ChangeColorBrightness(color, value);
            }
        }

        public static Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                Attributes.Color = Color;
                OnAttributesUpdated?.Invoke(new SolidColorBrush(Color)
                {
                    Opacity = Opacity
                });
                UpdateInkPresenters();
            }
        }

        private static void UpdateInkPresenters()
        {
            foreach (var presenter in Presenters)
            {
                presenter.UpdateDefaultDrawingAttributes(Attributes);
            }
        }

        public static double Hue
        {
            get { return _hue; }
            set
            {
                _hue = value;
                Color = ChangeColorBrightness(HsvToRgb(value, 1, 1), Brightness);
            }
        }

        public static double Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = value;
                if (Attributes.PencilProperties != null) Attributes.PencilProperties.Opacity = Opacity;
                OnAttributesUpdated?.Invoke(new SolidColorBrush(Color)
                {
                    Opacity = Opacity
                });
                UpdateInkPresenters();
            }
        }

        public static double Size
        {
            get { return _size; }
            set
            {
                _size = value;
                Attributes.Size = new Size(Size, Size);
                OnAttributesUpdated?.Invoke(new SolidColorBrush(Color)
                {
                    Opacity = Opacity
                });
                UpdateInkPresenters();
            }
        }

        public static InkDrawingAttributes Attributes
        {
            get => _attributes;
            set
            {
                _attributes = value;
                foreach (var presenter in Presenters)
                {
                    presenter.UpdateDefaultDrawingAttributes(_attributes);
                }
            }
        }

        public static ObservableCollection<InkPresenter> Presenters { get; set; } =
            new ObservableCollection<InkPresenter>();

        public static bool IsRecognitionEnabled
        {
            get => _isRecognitionEnabled;
            set
            {
                if (value != _isRecognitionEnabled)
                {
                    RecognitionChanged?.Invoke(value);
                    _isRecognitionEnabled = value;
                }
            }
        }

        public static ObservableCollection<FreeformInkControl> FreeformInkControls { get; set; } =
            new ObservableCollection<FreeformInkControl>();

        public static event InkInputChangedEventHandler InkInputChanged;

        public static event BooleanToggledEventHandler RecognitionChanged;

        public static Color ChangeColorBrightness(Color color, double brightness)
        {
            var newFactor = brightness / 50 - 1;
            double red = color.R;
            double green = color.G;
            double blue = color.B;

            if (newFactor < 0)
            {
                //newFactor += 1;
                red += red * newFactor;
                green += green * newFactor;
                blue += blue * newFactor;
            }
            else
            {
                red += (255 - red) * newFactor;
                green += (255 - green) * newFactor;
                blue += (255 - blue) * newFactor;
            }
            return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
        }

        /// <summary>
        /// Inverses the ChangeColorBrightness function to get the brightness factor for a color, given its hue
        /// </summary>
        /// <param name="color"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public static double GetBrightnessFromColor(Color color, double hue)
        {
            Color midColor = HsvToRgb(hue, 1, 1);
            double brightness;
            double x1;
            double x2;
            if (color.R == midColor.R)
            {
                if (color.B == midColor.B)
                {
                    if (color.G == midColor.G)
                    {
                        return 0;
                    }
                    x1 = midColor.G;
                    x2 = color.G;
                }
                else
                {
                    x1 = midColor.B;
                    x2 = color.B;
                }
            }
            else
            {
                x1 = midColor.R;
                x2 = color.R;
            }
            if (x1 > x2)
            {
                brightness = ((x2 - x1) / x1);
            }
            else
            {
                brightness = (x2 - x1) / (255 - x1);
            }
            return brightness;
        }

        public static void ForceUpdateFromAttributes(InkDrawingAttributes attributes)
        {
            Color color = attributes.Color;
            double h, s, v;
            RgbToHsV(color, out h,out s,out v);
            Hue = h;
            _brightness = (GetBrightnessFromColor(color, h) + 1) * 50;
            Size = attributes.Size.Width;
            if (attributes.PencilProperties != null)
            {
                StrokeType = StrokeTypes.Pencil;
                Opacity = attributes.PencilProperties.Opacity;
            }
            else StrokeType = StrokeTypes.Pen;
        }

        public static void RgbToHsV(Color rgb, out double h, out double s, out double v)
        {
            double delta, min;
            h = s = v = 0;

            min = Math.Min(Math.Min(rgb.R, rgb.G), rgb.B);
            v = Math.Max(Math.Max(rgb.R, rgb.G), rgb.B);
            delta = v - min;

            if (v == 0.0)
                s = 0;
            else
                s = delta / v;

            if (s == 0)
                h = 0.0;

            else
            {
                if (rgb.R == v)
                    h = (rgb.G - rgb.B) / delta;
                else if (rgb.G == v)
                    h = 2 + (rgb.B - rgb.R) / delta;
                else if (rgb.B == v)
                    h = 4 + (rgb.R - rgb.G) / delta;

                h *= 60;

                if (h < 0.0)
                    h = h + 360;
            }
        }

        public static Color HsvToRgb(double h, double s, double v)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            while (h < 0) { h += 360; };
            while (h >= 360) { h -= 360; };
            double R;
            double G;
            double B;
            if (v <= 0)
            { R = G = B = 0; }
            else if (s <= 0)
            {
                R = G = B = v;
            }
            else
            {
                double hf = h / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = v * (1 - s);
                double qv = v * (1 - s * f);
                double tv = v * (1 - s * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = v;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = v;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = v;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = v;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = v;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = v;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = v;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = v;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = v; // Just pretend its black/white
                        break;
                }
            }
            var r = Clamp((int)(R * 255.0));
            var g = Clamp((int)(G * 255.0));
            var b = Clamp((int)(B * 255.0));
            return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        public static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}