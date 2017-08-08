using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using System.Collections.Generic;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InkSettings : UserControl
    {
        public InkSettings()
        {
            this.InitializeComponent();
        }

        public InkSettings(DocumentController docController, Context context) : this()
        {
            Debug.Assert(docController.DocumentType == InkBox.DocumentType, "You can only create image settings for an InkBox");

            xSizeRow.Children.Add(new SizeSettings(docController, context));
            xPositionRow.Children.Add(new PositionSettings(docController, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(docController, context));
        }
        
    }
}
