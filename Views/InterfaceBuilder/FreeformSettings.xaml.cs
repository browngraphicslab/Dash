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

        public FreeformSettings()
        {
            this.InitializeComponent();
        }

        public FreeformSettings(DocumentController layoutDocument, DocumentController dataDocument, Context context): this()
        {
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
            xSizeRow.Children.Add(new SizeSettings(layoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(layoutDocument, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(layoutDocument,context));
        }
    }
}
