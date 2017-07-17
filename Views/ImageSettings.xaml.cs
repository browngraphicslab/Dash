using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using System.Collections.Generic;
using Dash.Views;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ImageSettings : UserControl
    {
        public ImageSettings()
        {
            this.InitializeComponent();
        }

        public ImageSettings(DocumentController docController, Context context) : this()
        {
            Debug.Assert(docController.DocumentType == CourtesyDocuments.ImageBox.DocumentType, "You can only create image settings for an ImageBox");

            var docContextList = context.DocContextList; 
            xSizeRow.Children.Add(new SizeSettings(docController, docContextList));
            xPositionRow.Children.Add(new PositionSettings(docController, docContextList));
            BindOpacity(docController, context);
            //BindWidth(docController, docContextList);
            //BindHeight(docController, docContextList);
            //BindPosition(docController, docContextList);
        }

        //private void BindPosition(DocumentController docController, IEnumerable<DocumentController> docContextList)
        //{
        //    var positionController = docController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, docContextList) as PointFieldModelController;
        //    Debug.Assert(positionController != null);

        //    var converter = new StringCoordinateToPointConverter(positionController.Data);

        //    var xPositionBinding = new Binding
        //    {
        //        Source = positionController,
        //        Path = new PropertyPath(nameof(positionController.Data)),
        //        Mode = BindingMode.TwoWay,
        //        Converter = converter,
        //        ConverterParameter = Coordinate.X,
        //        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        //    };
        //    xHorizontalPositionTextBox.SetBinding(TextBox.TextProperty, xPositionBinding);

        //    var yPositionBinding = new Binding
        //    {
        //        Source = positionController,
        //        Path = new PropertyPath(nameof(positionController.Data)),
        //        Mode = BindingMode.TwoWay,
        //        Converter = converter,
        //        ConverterParameter = Coordinate.Y,
        //        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        //    };
        //    xVerticalPositionTextBox.SetBinding(TextBox.TextProperty, yPositionBinding);
        //}

        //private void BindHeight(DocumentController docController, IEnumerable<DocumentController> docContextList)
        //{
        //    var heightController = docController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, docContextList) as NumberFieldModelController;
        //    Debug.Assert(heightController != null);

        //    var heightBinding = new Binding
        //    {
        //        Source = heightController,
        //        Path = new PropertyPath(nameof(heightController.Data)),
        //        Mode = BindingMode.TwoWay,
        //        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        //    };
        //    xHeightTextBox.SetBinding(TextBox.TextProperty, heightBinding);
        //}

        //private void BindWidth(DocumentController docController, IEnumerable<DocumentController> docContextList)
        //{
        //    var widthController = docController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, docContextList) as NumberFieldModelController;
        //    Debug.Assert(widthController != null);

        //    var widthBinding = new Binding
        //    {
        //        Source = widthController,
        //        Path = new PropertyPath(nameof(widthController.Data)),
        //        Mode = BindingMode.TwoWay,
        //        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        //    };
        //    xWidthTextBox.SetBinding(TextBox.TextProperty, widthBinding);
        //}


        private void BindOpacity(DocumentController docController, Context context)
        {
            var opacityController =
                    docController.GetDereferencedField(CourtesyDocuments.ImageBox.OpacityKey, context) as NumberFieldModelController;
            Debug.Assert(opacityController != null);

            var opacityBinding = new Binding()
            {
                Source = opacityController,
                Path = new PropertyPath(nameof(opacityController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xOpacitySliderTextbox.SetBinding(TextBox.TextProperty, opacityBinding);

            var textOpacityBinding = new Binding()
            {
                Source = xOpacitySliderTextbox,
                Path = new PropertyPath(nameof(xOpacitySliderTextbox.Text)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            xOpacitySlider.SetBinding(Slider.ValueProperty, textOpacityBinding);

        }
    }
}
