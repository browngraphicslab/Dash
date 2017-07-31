using System;
using System.Collections.Generic;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class AlignmentSettings : UserControl
    {

        private readonly DocumentController _editedLayoutDocument;

        public AlignmentSettings()
        {
            this.InitializeComponent();
        }

        public AlignmentSettings(DocumentController editedLayoutDocument, Context context): this()
        {
            _editedLayoutDocument = editedLayoutDocument;
            editedLayoutDocument.AddFieldUpdatedListener(CourtesyDocument.HorizontalAlignmentKey, HorizontalAlignmentChanged);
            editedLayoutDocument.AddFieldUpdatedListener(CourtesyDocument.VerticalAlignmentKey, VerticalAlignmentChanged);

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
            var saved = pos; 
            pos.Data = new Point(pos.Data.X, 0); 

            _editedLayoutDocument.SetVerticalAlignment(verticalAlignment);
            if (verticalAlignment == VerticalAlignment.Stretch)
            {
                var hf = _editedLayoutDocument.GetHeightField();
                hf.Data = double.NaN;
            }
            pos.Data = saved.Data; 
        }

        private void XHorizontalAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var horizontalAlignment = (HorizontalAlignment)xHorizontalAlignmentComboBox.SelectedIndex;
            if (horizontalAlignment == _editedLayoutDocument.GetHorizontalAlignment()) return;

            var pos = _editedLayoutDocument.GetPositionField();
            pos.Data = new Point(0, pos.Data.Y);

            _editedLayoutDocument.SetHorizontalAlignment(horizontalAlignment);
            if (horizontalAlignment == HorizontalAlignment.Stretch)
            {
                var wf = _editedLayoutDocument.GetWidthField();
                wf.Data = double.NaN;
            }
        }

        // called when the document key updates
        private void VerticalAlignmentChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var alignment = sender.GetVerticalAlignment();
            xVerticalAlignmentComboBox.SelectionChanged -= XVerticalAlignmentComboBox_SelectionChanged; ;
            xVerticalAlignmentComboBox.SelectedIndex = (int) alignment;
            xVerticalAlignmentComboBox.SelectionChanged += XVerticalAlignmentComboBox_SelectionChanged; ;
        }

        // called when the document key updates
        private void HorizontalAlignmentChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {

            var alignment = sender.GetHorizontalAlignment();
            xHorizontalAlignmentComboBox.SelectionChanged -= XHorizontalAlignmentComboBox_SelectionChanged;
            xHorizontalAlignmentComboBox.SelectedIndex = (int)alignment;
            xHorizontalAlignmentComboBox.SelectionChanged += XHorizontalAlignmentComboBox_SelectionChanged;

        }
    }
}
