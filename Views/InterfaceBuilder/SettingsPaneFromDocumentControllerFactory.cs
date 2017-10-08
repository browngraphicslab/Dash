﻿using System.Diagnostics;
using Windows.UI.Xaml;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public static class SettingsPaneFromDocumentControllerFactory
    {
        public static UIElement CreateSettingsPane(DocumentController layoutDocument, DocumentController dataDocument)
        {
            if (dataDocument != null)
            {
                return CreateDocumentSettingsLayout(layoutDocument, dataDocument);
            }
            var type = layoutDocument.DocumentType;
            if (type == ImageBox.DocumentType)
            {
                return CreateImageSettingsLayout(layoutDocument);
            }
            if (type == TextingBox.DocumentType)
            {
                return CreateTextSettingsLayout(layoutDocument);
            }
            if (type == CollectionBox.DocumentType)
            {
                return CreateCollectionSettingsLayout(layoutDocument);
            }
            if (type == RichTextBox.DocumentType)
            {
                return CreateRichTextSettingsLayout(layoutDocument);
            }
            if (type == DashConstants.DocumentTypeStore.FreeFormDocumentLayout)
            {
                return CreateDocumentSettingsLayout(layoutDocument, dataDocument);
            }
            if (type == ListViewLayout.DocumentType)
            {
                return CreateListViewSettingsLayout(layoutDocument);
            }
            if (type == GridViewLayout.DocumentType)
            {
                return CreateGridViewsettingsLayout(layoutDocument); 
            }
            if (type == InkBox.DocumentType)
            {
                return CreateInkSettingsLayout(layoutDocument);
            }
            if (type == KeyValueDocumentBox.DocumentType)
            {
                return CreateKeyValueSettingsLayout(layoutDocument);
            }

            Debug.WriteLine($"InterfaceBulder.xaml.cs.SettingsPaneFromDocumentControllerFactory: \n\tWe do not create a settings pane for the document with type {layoutDocument.DocumentType}");
            
            return null;
        }
        
        private static UIElement CreateKeyValueSettingsLayout(DocumentController layoutDocument)
        {
            var context = new Context();
            return new KeyValueSettings(layoutDocument, context);
        }
        
        private static UIElement CreateInkSettingsLayout(DocumentController layoutDocument)
        {
            var context = new Context();
            return new InkSettings(layoutDocument, context);
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

        private static UIElement CreateListViewSettingsLayout(DocumentController layoutDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new ListViewSettings(layoutDocument, context);
        }

        private static UIElement CreateGridViewsettingsLayout(DocumentController layoutDocument)
        {
            return new GridViewSettings(layoutDocument, new Context()); 
        }

        private static UIElement CreateTextSettingsLayout(DocumentController layoutDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new TextSettings(layoutDocument, context);
        }

        private static UIElement CreateDocumentSettingsLayout(DocumentController layoutDocument, DocumentController dataDocument)
        {
            var context = new Context(); // bcz: ??? Is this right?
            return new FreeformSettings(layoutDocument, dataDocument, context);
        }

    }
   
}
