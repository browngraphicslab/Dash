using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using DashShared;
using FontWeights = Windows.UI.Text.FontWeights;

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

        public TextSettings(DocumentController editedLayoutDocument) : this()
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

            BindWidth(editedLayoutDocument);
            BindHeight(editedLayoutDocument);
            BindPosition(editedLayoutDocument);
            BindFontWeight(editedLayoutDocument);
            BindFontSize(editedLayoutDocument);
            BindFontAlignment(editedLayoutDocument);
        }

        private void BindFontAlignment(DocumentController docController)
        {
            var textAlignmentController =
                docController.GetField(CourtesyDocuments.TextingBox.TextAlignmentKey) as NumberFieldModelController;
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

        private void BindFontWeight(DocumentController docController)
        {
            var fontWeightController =
                    docController.GetField(CourtesyDocuments.TextingBox.FontWeightKey) as NumberFieldModelController;
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

        private void BindFontSize(DocumentController docController)
        {
            var fontSizeController =
                    docController.GetField(CourtesyDocuments.TextingBox.FontSizeKey) as NumberFieldModelController;
            Debug.Assert(fontSizeController != null);

            var fontSizeBinding = new Binding()
            {
                Source = fontSizeController,
                Path = new PropertyPath(nameof(fontSizeController.Data)),
                Mode = BindingMode.TwoWay,
            };

            xFontSizeSlider.SetBinding(Slider.ValueProperty, fontSizeBinding);
            xFontSizeTextBox.SetBinding(TextBox.TextProperty, fontSizeBinding);
        }

        private void BindPosition(DocumentController docController)
        {
            var positionController = docController.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
            Debug.Assert(positionController != null);

            var converter = new StringCoordinateToPointConverter(positionController.Data);

            var xPositionBinding = new Binding
            {
                Source = positionController,
                Path = new PropertyPath(nameof(positionController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = converter,
                ConverterParameter = Coordinate.X
            };
            xHorizontalPositionTextBox.SetBinding(TextBox.TextProperty, xPositionBinding);

            var yPositionBinding = new Binding
            {
                Source = positionController,
                Path = new PropertyPath(nameof(positionController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = converter,
                ConverterParameter = Coordinate.Y
            };
            xVerticalPositionTextBox.SetBinding(TextBox.TextProperty, yPositionBinding);
        }

        private void BindHeight(DocumentController docController)
        {
            var heightController = docController.GetField(DashConstants.KeyStore.HeightFieldKey) as NumberFieldModelController;
            Debug.Assert(heightController != null);

            var heightBinding = new Binding
            {
                Source = heightController,
                Path = new PropertyPath(nameof(heightController.Data)),
                Mode = BindingMode.TwoWay
            };
            xHeightTextBox.SetBinding(TextBox.TextProperty, heightBinding);
        }

        private void BindWidth(DocumentController docController)
        {
            var widthController = docController.GetField(DashConstants.KeyStore.WidthFieldKey) as NumberFieldModelController;
            Debug.Assert(widthController != null);

            var widthBinding = new Binding
            {
                Source = widthController,
                Path = new PropertyPath(nameof(widthController.Data)),
                Mode = BindingMode.TwoWay
            };
            xWidthTextBox.SetBinding(TextBox.TextProperty, widthBinding);
        }
    }
}
