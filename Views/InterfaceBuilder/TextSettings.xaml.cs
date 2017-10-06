using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using FontWeights = Windows.UI.Text.FontWeights;
using System.Collections.Generic;
using Dash.Converters;
using Windows.UI;
using System.Reflection;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TextSettings : UserControl
    {
        private ObservableCollection<string> _fontWeights = new ObservableCollection<string>();
        ObservableCollection<NamedColor> colors { get; set; }

        public TextSettings()
        {
            this.InitializeComponent();
            this.AddColors();

        }

        public TextSettings(DocumentController editedLayoutDocument, Context context) : this()
        {            
            xSizeRow.Children.Add(new SizeSettings(editedLayoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(editedLayoutDocument, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(editedLayoutDocument,context));
            BindFontWeight(editedLayoutDocument, context);
            BindFontSize(editedLayoutDocument, context);
            BindFontAlignment(editedLayoutDocument, context);
            xBackgroundColorComboBox.SelectionChanged += delegate
            {
                this.ColorSelectionChanged(editedLayoutDocument, context);
            };
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
                    docController.GetDereferencedField(TextingBox.FontWeightKey, context) as TextFieldModelController;
            Debug.Assert(fontWeightController != null);

            _fontWeights = new ObservableCollection<string>()
            {
                "Black", //FontWeights.Black.Weight,
                "Bold", //FontWeights.Bold.Weight,
                "Normal", // FontWeights.Normal.Weight,
                "Light" //FontWeights.Light.Weight
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

        private void ColorSelectionChanged(DocumentController docController, Context context)
        {
            var textController = docController.GetDereferencedField(TextingBox.BackgroundColorKey, context) as TextFieldModelController;
            Debug.Assert(textController != null);
            var col = (xBackgroundColorComboBox.SelectedItem as NamedColor).Color;
            docController.SetField(TextingBox.BackgroundColorKey, new TextFieldModelController(col.ToString()), true);
        }
        

        private void AddColors()
        {
            if (colors == null) colors = new ObservableCollection<NamedColor>();
            foreach (var color in typeof(Colors).GetRuntimeProperties())
            {
                colors.Add(new NamedColor() { Name = color.Name, Color = (Color)color.GetValue(null) });
            }
        }
    }
}
