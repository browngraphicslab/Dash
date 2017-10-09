using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValueSettings : UserControl
    {
        public KeyValueSettings()
        {
            this.InitializeComponent();
        }
        public KeyValueSettings(DocumentController docController, Context context) : this()
        {
            Debug.Assert(docController.DocumentType == KeyValueDocumentBox.DocumentType, "You can only create image settings for an InkBox");

            xSizeRow.Children.Add(new SizeSettings(docController, context));
            xPositionRow.Children.Add(new PositionSettings(docController, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(docController, context));

        }
    }
}
