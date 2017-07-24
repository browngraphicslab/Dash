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
        }

        private void XVerticalAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            _editedLayoutDocument.SetVerticalAlignment((VerticalAlignment)xVerticalAlignmentComboBox.SelectedIndex);

        }

        private void XHorizontalAlignmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _editedLayoutDocument.SetHorizontalAlignment((HorizontalAlignment)xHorizontalAlignmentComboBox.SelectedIndex);

        }

        private void VerticalAlignmentChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var alignment = sender.GetVerticalAlignment();
            xVerticalAlignmentComboBox.SelectionChanged -= XVerticalAlignmentComboBox_SelectionChanged; ;
            xVerticalAlignmentComboBox.SelectedIndex = (int) alignment;
            xVerticalAlignmentComboBox.SelectionChanged += XVerticalAlignmentComboBox_SelectionChanged; ;
        }

        private void HorizontalAlignmentChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {

            var alignment = sender.GetHorizontalAlignment();
            xHorizontalAlignmentComboBox.SelectionChanged -= XHorizontalAlignmentComboBox_SelectionChanged;
            xHorizontalAlignmentComboBox.SelectedIndex = (int)alignment;
            xHorizontalAlignmentComboBox.SelectionChanged += XHorizontalAlignmentComboBox_SelectionChanged;

        }
    }
}
