using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class PositionSettings : UserControl
    {
        private TextBox _horizontalPositionTextBox;
        private TextBox _verticalPositionTextBox;
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
            var fmc = docController.GetField(DashConstants.KeyStore.PositionFieldKey); 
            var positionController = DocumentController.GetDereferencedField(fmc, fmc.Context) as PointFieldModelController;
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
            xHorizontalPositionTextBox.SetBinding(TextBox.TextProperty,xPositionBinding);

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
    }
}
