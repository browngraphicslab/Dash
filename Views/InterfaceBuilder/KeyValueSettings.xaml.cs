using System.Diagnostics;
using Windows.UI.Xaml.Controls;

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
            Debug.Assert(docController.DocumentType.Equals(KeyValueDocumentBox.DocumentType), "You can only create image settings for an InkBox");

            xSizeRow.Children.Add(new SizeSettings(docController, context));
            xPositionRow.Children.Add(new PositionSettings(docController, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(docController, context));

        }
    }
}
