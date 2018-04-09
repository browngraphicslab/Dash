using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class AlignmentSettings : UserControl
    {

        private readonly DocumentController _editedLayoutDocument;

        private double _tempHeight;
        private double _tempWidth; 

        public AlignmentSettings()
        {
            this.InitializeComponent();
        }

        public AlignmentSettings(DocumentController editedLayoutDocument, Context context): this()
        {
            _editedLayoutDocument = editedLayoutDocument;
            _tempHeight = editedLayoutDocument.GetHeightField().Data;
            _tempWidth = editedLayoutDocument.GetWidthField().Data; 

            editedLayoutDocument.AddFieldUpdatedListener(KeyStore.HorizontalAlignmentKey, HorizontalAlignmentChanged);
            editedLayoutDocument.AddFieldUpdatedListener(KeyStore.VerticalAlignmentKey, VerticalAlignmentChanged);

            xHorizontalAlignmentComboBox.SelectionChanged += XHorizontalAlignmentComboBox_SelectionChanged;
            xVerticalAlignmentComboBox.SelectionChanged += XVerticalAlignmentComboBox_SelectionChanged; ;
            SetActiveVerticalAlignment();
            SetActiveHorizontalAlignment();
        }

        private void SetActiveHorizontalAlignment()
        {            
            var horizontalAlignment = _editedLayoutDocument.GetHorizontalAlignment();
            var selectedItem = xHorizontalAlignmentComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(cbi => (cbi.Content as string).Equals(horizontalAlignment.ToString()));
            xHorizontalAlignmentComboBox.SelectedItem = selectedItem;
        }

        private void SetActiveVerticalAlignment()
        {
            var verticalAlignment = _editedLayoutDocument.GetVerticalAlignment();
            var selectedItem = xVerticalAlignmentComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(cbi => (cbi.Content as string).Equals(verticalAlignment.ToString()));
            xVerticalAlignmentComboBox.SelectedItem = selectedItem;
        }

        

        private void XVerticalAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var verticalAlignment = (VerticalAlignment)xVerticalAlignmentComboBox.SelectedIndex;
            if (verticalAlignment == _editedLayoutDocument.GetVerticalAlignment()) return;

            var pos = _editedLayoutDocument.GetPositionField();
            pos.Data = new Point(pos.Data.X, 0);

            if (Double.IsNaN(_editedLayoutDocument.GetHeightField().Data)) _editedLayoutDocument.SetHeight(_tempHeight);
            _editedLayoutDocument.SetVerticalAlignment(verticalAlignment);
            if (verticalAlignment == VerticalAlignment.Stretch)
            {
                var hf = _editedLayoutDocument.GetHeightField();
                _tempHeight = hf.Data; 
                hf.Data = double.NaN;
            }
        }

        private void XHorizontalAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var horizontalAlignment = (HorizontalAlignment)xHorizontalAlignmentComboBox.SelectedIndex;
            if (horizontalAlignment == _editedLayoutDocument.GetHorizontalAlignment()) return;

            var pos = _editedLayoutDocument.GetPositionField();
            pos.Data = new Point(0, pos.Data.Y);

            if (Double.IsNaN(_editedLayoutDocument.GetWidthField().Data)) _editedLayoutDocument.SetWidth(_tempWidth); 
            _editedLayoutDocument.SetHorizontalAlignment(horizontalAlignment);
            if (horizontalAlignment == HorizontalAlignment.Stretch)
            {
                var wf = _editedLayoutDocument.GetWidthField();
                _tempWidth = wf.Data; 
                wf.Data = double.NaN;
            }
        }

        // called when the document key updates
        private void VerticalAlignmentChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
            var alignment = ((DocumentController)sender).GetVerticalAlignment();
            xVerticalAlignmentComboBox.SelectionChanged -= XVerticalAlignmentComboBox_SelectionChanged; ;
            xVerticalAlignmentComboBox.SelectedIndex = (int) alignment;
            xVerticalAlignmentComboBox.SelectionChanged += XVerticalAlignmentComboBox_SelectionChanged; ;
        }

        // called when the document key updates
        private void HorizontalAlignmentChanged(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {

            var alignment = ((DocumentController)sender).GetHorizontalAlignment();
            xHorizontalAlignmentComboBox.SelectionChanged -= XHorizontalAlignmentComboBox_SelectionChanged;
            xHorizontalAlignmentComboBox.SelectedIndex = (int)alignment;
            xHorizontalAlignmentComboBox.SelectionChanged += XHorizontalAlignmentComboBox_SelectionChanged;

        }
    }
}
