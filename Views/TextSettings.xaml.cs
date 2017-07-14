using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using FontWeights = Windows.UI.Text.FontWeights;
using System.Collections.Generic;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TextSettings : UserControl
    {
        private DocumentController editedLayoutDocument;
        private ObservableCollection<double> _fontWeights = new ObservableCollection<double>();

        public TextSettings()
        {
            this.InitializeComponent();
        }

        public TextSettings(DocumentController editedLayoutDocument, Context context) : this()
        {
            this.editedLayoutDocument = editedLayoutDocument;

            //Border LeftButton = new Border()
            //{
            //    Child = new Image()
            //    {
            //        Source =
            //            new BitmapImage(new Uri(@"\Assets\Leftlg.png"))
            //    },
            //    //Child = new TextBlock() { Text = "Left" },
            //    Width = 30,
            //    Height = 20
            //};
            //Border CenterButton = new Border()
            //{
            //    Child = new Image()
            //    {
            //        Source =
            //            new BitmapImage(new Uri(@"\Assets\Centerlg.png"))
            //    },
            //    //Child = new TextBlock() { Text = "Center" },
            //    Width = 30,
            //    Height = 20
            //};
            //Border RightButton = new Border()
            //{
            //    Child = new Image()
            //    {
            //        Source =
            //            new BitmapImage(new Uri(@"\Assets\Rightlg.png"))
            //    },
            //    //Child = new TextBlock() { Text = "Right" },
            //    Width = 30,
            //    Height = 20
            //};

            //xAlignmentListView.ItemsSource = new List<UIElement>
            //{
            //    LeftButton,
            //    CenterButton,
            //    RightButton
            //};

            BindWidth(editedLayoutDocument, context);
            BindHeight(editedLayoutDocument, context);
            BindPosition(editedLayoutDocument, context);
            BindFontWeight(editedLayoutDocument, context);
            BindFontSize(editedLayoutDocument, context);
            BindFontAlignment(editedLayoutDocument, context);
        }

        private void BindFontAlignment(DocumentController docController, Context context)
        {
            var textAlignmentController =
                docController.GetDereferencedField(CourtesyDocuments.TextingBox.TextAlignmentKey, context) as NumberFieldModelController;
            Debug.Assert(textAlignmentController != null);

            var fontAlignmentBinding = new Binding()
            {
                Source = textAlignmentController,
                Path = new PropertyPath(nameof(textAlignmentController.Data)),
                Mode = BindingMode.TwoWay,
            };

            xAlignmentListView.SetBinding(ListView.SelectedIndexProperty, fontAlignmentBinding);
            xAlignmentListView.SelectionChanged += delegate(object sender, SelectionChangedEventArgs args) { Debug.WriteLine(xAlignmentListView.SelectedIndex); };
        }

        private void BindFontWeight(DocumentController docController, Context context)
        {
            var fontWeightController =
                    docController.GetDereferencedField(CourtesyDocuments.TextingBox.FontWeightKey, context) as NumberFieldModelController;
            Debug.Assert(fontWeightController != null);

            _fontWeights = new ObservableCollection<double>()
            {
                FontWeights.Black.Weight,
                FontWeights.Bold.Weight,
                FontWeights.Normal.Weight,
                FontWeights.Light.Weight
            };
            xFontWeightBox.ItemsSource = _fontWeights;

            var FontWeightBinding = new Binding()
            {
                Source = fontWeightController,
                Path = new PropertyPath(nameof(fontWeightController.Data)),
                Mode = BindingMode.TwoWay,
            };

            xFontWeightBox.SetBinding(ComboBox.SelectedValueProperty, FontWeightBinding);
        }

        private void BindFontSize(DocumentController docController, Context context)
        {
            var fontSizeController =
                    docController.GetDereferencedField(CourtesyDocuments.TextingBox.FontSizeKey, context) as NumberFieldModelController;
            Debug.Assert(fontSizeController != null);

            var fontSizeBinding = new Binding()
            {
                Source = fontSizeController,
                Path = new PropertyPath(nameof(fontSizeController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            xFontSizeSlider.SetBinding(Slider.ValueProperty, fontSizeBinding);
            xFontSizeTextBox.SetBinding(TextBox.TextProperty, fontSizeBinding);
        }

        private void BindPosition(DocumentController docController, Context context)
        {
            var positionController = docController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, context) as PointFieldModelController;
            Debug.Assert(positionController != null);

            var converter = new StringCoordinateToPointConverter(positionController.Data);

            var xPositionBinding = new Binding
            {
                Source = positionController,
                Path = new PropertyPath(nameof(positionController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = converter,
                ConverterParameter = Coordinate.X,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xHorizontalPositionTextBox.SetBinding(TextBox.TextProperty, xPositionBinding);

            var yPositionBinding = new Binding
            {
                Source = positionController,
                Path = new PropertyPath(nameof(positionController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = converter,
                ConverterParameter = Coordinate.Y,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xVerticalPositionTextBox.SetBinding(TextBox.TextProperty, yPositionBinding);
        }

        private void BindHeight(DocumentController docController, Context context)
        {
            var heightController = docController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, context) as NumberFieldModelController;
            Debug.Assert(heightController != null);

            var heightBinding = new Binding
            {
                Source = heightController,
                Path = new PropertyPath(nameof(heightController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xHeightTextBox.SetBinding(TextBox.TextProperty, heightBinding);
        }

        private void BindWidth(DocumentController docController, Context context)
        {
            var widthController = docController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, context) as NumberFieldModelController;
            Debug.Assert(widthController != null);

            var widthBinding = new Binding
            {
                Source = widthController,
                Path = new PropertyPath(nameof(widthController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xWidthTextBox.SetBinding(TextBox.TextProperty, widthBinding);
        }
    }
}
