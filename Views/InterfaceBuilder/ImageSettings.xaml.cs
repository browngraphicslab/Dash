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
            Debug.Assert(docController.DocumentType == ImageBox.DocumentType, "You can only create image settings for an ImageBox");

            xSizeRow.Children.Add(new SizeSettings(docController, context));
            xPositionRow.Children.Add(new PositionSettings(docController, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(docController, context));
            BindOpacity(docController, context);
        }

        private void BindOpacity(DocumentController docController, Context context)
        {
            var opacityController =
                    docController.GetDereferencedField(ImageBox.OpacityKey, context) as NumberFieldModelController;
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
