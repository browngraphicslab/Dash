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
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FreeformSettings : UserControl
    {
        private readonly DocumentController _dataDocument;
        private readonly Context _context;

        public DocumentController SelectedDocument { get; set; }

        public FreeformSettings()
        {
            this.InitializeComponent();
        }

        public FreeformSettings(DocumentController layoutDocument, DocumentController dataDocument, Context context): this()
        {
            SelectedDocument = layoutDocument;

            if (dataDocument == null)
            {
                xCollapsableDocRow.Height = new GridLength(0);
                TypeBlock.Text = layoutDocument.DocumentType.Type;
            }
            else
            {
                xDocRow.Children.Add(new DocumentSettings(layoutDocument, dataDocument, context));
                TypeBlock.Text =  "Document (" + layoutDocument.DocumentType.Type + ")";
            }

            if (layoutDocument.DocumentType.Type == "Spacing Layout")
            {
                xSpacingGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                GridViewSettings.BindSpacing(layoutDocument, context, xSpacingSliderTextBox, xSpacingSlider); 
            } else if (layoutDocument.DocumentType.Type == "ListView Layout")
            {
                xSpacingGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                ListViewSettings.BindSpacing(layoutDocument, context, xSpacingSliderTextBox, xSpacingSlider); 
            }

            xSizeRow.Children.Add(new SizeSettings(layoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(layoutDocument, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(layoutDocument,context));
        }
    }
}
