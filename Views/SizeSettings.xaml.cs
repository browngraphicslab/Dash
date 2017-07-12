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
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class SizeSettings : UserControl
    {
        public SizeSettings()
        {
            this.InitializeComponent();
        }

        public SizeSettings(DocumentController editedLayoutDocument, IEnumerable<DocumentController> docContextList) : this()
        {
            BindWidth(editedLayoutDocument, docContextList);
            BindHeight(editedLayoutDocument, docContextList);
        }

        private void XTitle_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            xContentGridColumn.Visibility = xContentGridColumn.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Collapsed;
            xCollapse.Visibility = xCollapse.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Collapsed;
            xExpand.Visibility = xExpand.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BindHeight(DocumentController docController, IEnumerable<DocumentController> docContextList)
        {
            var heightController = docController.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, docContextList) as NumberFieldModelController;
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

        private void BindWidth(DocumentController docController, IEnumerable<DocumentController> docContextList)
        {
            var widthController = docController.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, docContextList) as NumberFieldModelController;
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
