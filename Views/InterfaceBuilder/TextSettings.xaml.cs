using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using FontWeights = Windows.UI.Text.FontWeights;
using System.Collections.Generic;
using Dash.Converters;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TextSettings : UserControl
    {
        private ObservableCollection<double> _fontWeights = new ObservableCollection<double>();

        public TextSettings()
        {
            this.InitializeComponent();
        }

        public TextSettings(DocumentController editedLayoutDocument, Context context) : this()
        {            
            xSizeRow.Children.Add(new SizeSettings(editedLayoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(editedLayoutDocument, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(editedLayoutDocument,context));
            BindFontWeight(editedLayoutDocument, context);
            BindFontSize(editedLayoutDocument, context);
            BindFontAlignment(editedLayoutDocument, context);
        }

        private void BindFontAlignment(DocumentController docController, Context context)
        {
            var textAlignmentController =
                docController.GetDereferencedField(TextingBox.TextAlignmentKey, context) as NumberFieldModelController;
            Debug.Assert(textAlignmentController != null);

            var fontAlignmentBinding = new Binding()
            {
                Source = textAlignmentController,
                Path = new PropertyPath(nameof(textAlignmentController.Data)),
                Mode = BindingMode.TwoWay
            };

            xAlignmentListView.SetBinding(ListView.SelectedIndexProperty, fontAlignmentBinding);
            xAlignmentListView.SelectionChanged += delegate (object sender, SelectionChangedEventArgs args) { Debug.WriteLine(xAlignmentListView.SelectedIndex); };
        }

        private void BindFontWeight(DocumentController docController, Context context)
        {
            var fontWeightController =
                    docController.GetDereferencedField(TextingBox.FontWeightKey, context) as NumberFieldModelController;
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
                    docController.GetDereferencedField(TextingBox.FontSizeKey, context) as NumberFieldModelController;
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
    }
}
