using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;

namespace Dash
{
    public static class GlobalInkSettings
    {
        private static ObservableCollection<InkPresenter> _presenters = new ObservableCollection<InkPresenter>();
        private static InkDrawingAttributes _attributes = new InkDrawingAttributes();
        private static double _opacity = 1;
        private static double _size = 3;
        private static Color _color = Colors.DarkGray;
        private static StrokeTypes _strokeType;
        private static CoreInputDeviceTypes _inkInputType;

        public delegate void InkInputChangedEventHandler(CoreInputDeviceTypes newInputType);

        public static event InkInputChangedEventHandler InkInputChanged;

        public enum StrokeTypes
        {
            Pen,
            Pencil,
            Eraser
        }

        public static StrokeTypes StrokeType
        {
            get { return _strokeType; }
            set { _strokeType = value; }
        }

        public static CoreInputDeviceTypes InkInputType
        {
            get { return _inkInputType; }
            set
            {
                _inkInputType = value;
                foreach (var inkPresenter in Presenters)
                {
                    inkPresenter.InputDeviceTypes = value;
                }
                InkInputChanged?.Invoke(value);
            }
        }

        public static double BrightnessFactor { get; set; }

        public static Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
            }
        }

        public static double Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = value;
            }
        }

        public static double Size
        {
            get { return _size; }
            set
            {
                _size = value;
            }
        }

        public static InkDrawingAttributes Attributes
        {
            get { return _attributes; }
            set
            {
                _attributes = value;
                foreach (var presenter in Presenters)
                {
                    presenter.UpdateDefaultDrawingAttributes(_attributes);
                }
            }
        }

        public static ObservableCollection<InkPresenter> Presenters
        {
            get { return _presenters; }
            set
            {
                _presenters = value;
                foreach (var presenter in _presenters)
                {
                    presenter.UpdateDefaultDrawingAttributes(_attributes);
                    presenter.InputDeviceTypes = InkInputType;
                }
            }
        } 

        private static Color ChangeColorBrightness()
        {
            double newFactor = BrightnessFactor/50 - 1;
            double red = (float)Color.R;
            double green = (float)Color.G;
            double blue = (float)Color.B;

            if (newFactor < 0)
            {
                newFactor += 1;
                red *= newFactor;
                green *= newFactor;
                blue *= newFactor;
            }
            else
            {
                red = (255 - red) * newFactor + red;
                green = (255 - green) * newFactor + green;
                blue = (255 - blue) * newFactor + blue;
            }

            return Color.FromArgb(Color.A, (byte) red, (byte) green, (byte) blue);
        }

        public static void SetAttributes()
        {
            
            if (StrokeType == StrokeTypes.Eraser)
            {
                foreach (var presenter in Presenters)
                {
                    presenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
                }
                return;
            }
            foreach (var presenter in Presenters)
            {
                presenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
            }
            InkDrawingAttributes attributes = new InkDrawingAttributes();
            if (StrokeType == StrokeTypes.Pencil)
            {
                attributes = InkDrawingAttributes.CreateForPencil();
                attributes.PencilProperties.Opacity = GlobalInkSettings.Opacity;
            }
            attributes.Color = ChangeColorBrightness();
            attributes.Size = new Size(GlobalInkSettings.Size, GlobalInkSettings.Size);
            GlobalInkSettings.Attributes = attributes;

        }
    }
}
