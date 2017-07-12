using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class PositionSettings : UserControl
    {
        public PositionSettings()
        {
            this.InitializeComponent();
        }

        public PositionSettings(DocumentController editedLayoutDocument, IEnumerable<DocumentController> docContextList)
            : this()
        {
            BindPosition(editedLayoutDocument, docContextList);
        }

        private void BindPosition(DocumentController docController, IEnumerable<DocumentController> docContextList)
        {
            var positionController = docController.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, docContextList) as PointFieldModelController;
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
    }
}
