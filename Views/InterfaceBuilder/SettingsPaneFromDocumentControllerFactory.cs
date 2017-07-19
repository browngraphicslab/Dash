using System.Diagnostics;
using Windows.UI.Xaml;
using static Dash.CourtesyDocuments;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public static class SettingsPaneFromDocumentControllerFactory
    {
        public static UIElement CreateSettingsPane(DocumentController editedLayoutDocument)
        {
            if (editedLayoutDocument.DocumentType == ImageBox.DocumentType)
            {
                return CreateImageSettingsLayout(editedLayoutDocument);
            }
            if (editedLayoutDocument.DocumentType == TextingBox.DocumentType)
            {
                return CreateTextSettingsLayout(editedLayoutDocument);
            }

            Debug.WriteLine($"InterfaceBulder.xaml.cs.SettingsPaneFromDocumentControllerFactory: \n\tWe do not create a settings pane for the document with type {editedLayoutDocument.DocumentType}");
            return null;
        }

        private static UIElement CreateImageSettingsLayout(DocumentController editedLayoutDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new ImageSettings(editedLayoutDocument, context);
        }

        private static UIElement CreateTextSettingsLayout(DocumentController editedLayoutDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new TextSettings(editedLayoutDocument, context);
        }

    }
}
