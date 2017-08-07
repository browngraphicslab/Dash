using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SizeSettings : UserControl
    {

        public SizeSettings()
        {
            this.InitializeComponent();
        }


        public SizeSettings(DocumentController editedLayoutDocument, Context context) : this()
        {
            BindWidth(editedLayoutDocument, context);
            BindHeight(editedLayoutDocument, context);
        }

        private void BindHeight(DocumentController docController, Context context)
        {
            var heightController =
                docController.GetHeightField(context);
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
            var widthController = docController.GetWidthField(context);
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
