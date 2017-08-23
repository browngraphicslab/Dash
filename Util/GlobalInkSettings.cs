using System.Collections.ObjectModel;
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
            Eraser
        }

        private static InkDrawingAttributes _attributes = new InkDrawingAttributes();
        private static CoreInputDeviceTypes _inkInputType;
        private static bool _isSelectionEnabled;
        private static bool _isRecognitionEnabled;
        private static Color _color = Colors.DarkGray;

        public static StrokeTypes StrokeType { get; set; }

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

        public static bool IsSelectionEnabled
        {
            get => _isSelectionEnabled;
            set
            {
                _isSelectionEnabled = value;
                UpdateInkPresenters();
            }
        }

        public static double Brightness { get; set; }

        public static Color Color
        {
            get => ChangeColorBrightness();
            set => _color = value;
        }

        public static double Opacity { get; set; } = 1;

        public static double Size { get; set; } = 3;

        public static InkDrawingAttributes Attributes
        {
            get => _attributes;
            set
            {
                _attributes = value;
                foreach (var presenter in Presenters)
                    presenter.UpdateDefaultDrawingAttributes(_attributes);
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
                    UpdateInkPresenters();
                }
            }
        }

        public static ObservableCollection<FreeformInkControl> FreeformInkControls { get; set; } =
            new ObservableCollection<FreeformInkControl>();

        public static event InkInputChangedEventHandler InkInputChanged;

        public static event BooleanToggledEventHandler RecognitionChanged;

        private static Color ChangeColorBrightness()
        {
            var newFactor = Brightness / 50 - 1;
            double red = _color.R;
            double green = _color.G;
            double blue = _color.B;

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

            return Color.FromArgb(_color.A, (byte) red, (byte) green, (byte) blue);
        }

        public static void UpdateInkPresenters(bool? isSelectionEnabled = null)
        {
            if (isSelectionEnabled != null) IsSelectionEnabled = (bool) isSelectionEnabled;
            foreach (var cntrls in FreeformInkControls)
                cntrls.UpdateSelectionMode();
            if (IsSelectionEnabled)
                return;
            if (StrokeType == StrokeTypes.Eraser)
            {
                foreach (var presenter in Presenters)
                    presenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
                OnAttributesUpdated?.Invoke(new SolidColorBrush(Color)
                {
                    Opacity = Opacity
                });
                return;
            }
            var attributes = new InkDrawingAttributes();

            foreach (var presenter in Presenters)
                presenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
            if (StrokeType == StrokeTypes.Pencil)
            {
                attributes = InkDrawingAttributes.CreateForPencil();
                if (!IsRecognitionEnabled) attributes.PencilProperties.Opacity = Opacity;
            }
            //if (IsRecognitionEnabled)
            //{
            //    attributes.Color = ((SolidColorBrush) Application.Current.Resources["WindowsBlue"]).Color;
            //    attributes.Size = new Size(4, 4);
            //    Attributes = attributes;
            //    return;
            //}
            attributes.Color = Color;
            attributes.Size = new Size(Size, Size);
            Attributes = attributes;
            OnAttributesUpdated?.Invoke(new SolidColorBrush(Color)
            {
                Opacity = Opacity
            });
        }
    }
}