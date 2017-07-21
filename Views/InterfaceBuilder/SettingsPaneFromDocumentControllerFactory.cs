using System.Diagnostics;
using Windows.UI.Xaml;
using Dash.Views;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public static class SettingsPaneFromDocumentControllerFactory
    {
        public static UIElement CreateSettingsPane(DocumentController layoutDocument, DocumentController dataDocument)
        {
            if (layoutDocument.DocumentType == ImageBox.DocumentType)
            {
                return CreateImageSettingsLayout(layoutDocument);
            }
            if (layoutDocument.DocumentType == TextingBox.DocumentType)
            {
                return CreateTextSettingsLayout(layoutDocument);
            }
            if (layoutDocument.DocumentType == CollectionBox.DocumentType)
            {
                return CreateCollectionSettingsLayout(layoutDocument);
            }
            if (layoutDocument.DocumentType == RichTextBox.DocumentType)
            {
                return CreateRichTextSettingsLayout(layoutDocument);
            }

            Debug.WriteLine($"InterfaceBulder.xaml.cs.SettingsPaneFromDocumentControllerFactory: \n\tWe do not create a settings pane for the document with type {layoutDocument.DocumentType}");
            if (dataDocument != null)
            {
                return CreateDocumentSettingsLayout(layoutDocument, dataDocument);
            }
            return null;
        }

        private static UIElement CreateRichTextSettingsLayout(DocumentController layoutDocument)
        {
            var context = new Context();
            return new RichTextSettings(layoutDocument, context);
        }

        private static UIElement CreateCollectionSettingsLayout(DocumentController layoutDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new CollectionSettings(layoutDocument, context);
        }

        private static UIElement CreateImageSettingsLayout(DocumentController layoutDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new ImageSettings(layoutDocument, context);
        }

        private static UIElement CreateTextSettingsLayout(DocumentController layoutDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new TextSettings(layoutDocument, context);
        }

        private static UIElement CreateDocumentSettingsLayout(DocumentController layoutDocument, DocumentController dataDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new DocumentSettings(layoutDocument, dataDocument, context);
        }

    }
}
