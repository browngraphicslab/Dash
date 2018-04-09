using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionSettings : UserControl
    {
        public CollectionSettings()
        {
            this.InitializeComponent();
        }


        public CollectionSettings(DocumentController editedLayoutDocument, Context context): this()
        {
            xSizeRow.Children.Add(new SizeSettings(editedLayoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(editedLayoutDocument, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(editedLayoutDocument,context));
        }
    }
}
