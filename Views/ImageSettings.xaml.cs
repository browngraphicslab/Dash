using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Navigation;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ImageSettings : UserControl
    {
        private DocumentController _docController;

        public ImageSettings()
        {
            this.InitializeComponent();
        }

        public ImageSettings(DocumentController docController) : this()
        {
            Debug.Assert(docController.DocumentType == CourtesyDocuments.ImageBox.DocumentType, "You can only create image settings for an ImageBox");
            _docController = docController;

            BindOpacity(_docController);
            BindWidth(_docController);
            BindHeight(_docController);
            BindPosition(_docController);

        }

        private void BindPosition(DocumentController docController)
        {
            var positionController = docController.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
            Debug.Assert(positionController != null);

            var xPositionBinding = new Binding
            {
                Source = positionController,
                Path = new PropertyPath(nameof(positionController.Data)),
                Mode = BindingMode.TwoWay
            };
            xHorizontalPositionTextBox.SetBinding(TextBox.TextProperty, xPositionBinding);
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

        private void BindOpacity(DocumentController docController)
        {
            var opacityController =
                    docController.GetField(CourtesyDocuments.ImageBox.OpacityKey) as NumberFieldModelController;
            Debug.Assert(opacityController != null);

            var opacityBinding = new Binding()
            {
                Source = opacityController,
                Path = new PropertyPath(nameof(opacityController.Data)),
                Mode = BindingMode.TwoWay
            };
            xOpacitySliderTextbox.SetBinding(TextBox.TextProperty, opacityBinding);

            var textOpacityBinding = new Binding()
            {
                Source = xOpacitySliderTextbox,
                Path = new PropertyPath(nameof(xOpacitySliderTextbox.Text)),
                Mode = BindingMode.TwoWay
            };

            xOpacitySlider.SetBinding(Slider.ValueProperty, textOpacityBinding);

        }
    }
}
