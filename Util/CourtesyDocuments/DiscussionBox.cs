using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using DashShared;
using Dash.Views;
using Microsoft.Toolkit.Uwp.UI.Controls;

namespace Dash
{
    public class DiscussionBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("4218B5EA-230E-4DD2-99C5-A4285626CC28", "Discussion Box");
        private static readonly string PrototypeId = "A66C951B-9B4B-4CB1-9493-E18877907B76";

        public DiscussionBox(FieldControllerBase refToDiscussion, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToDiscussion);
            SetupDocument(DocumentType, PrototypeId, "DiscussionBox Prototype Layout", fields);
        }
        public static FrameworkElement MakeView(DocumentController docController, KeyController key, Context context)
        {
            // create the image
            var editableTree = new DiscussionView();
            // setup bindings on the image
            SetupBinding(editableTree, docController, key, context);

            return editableTree;
        }
        public static void SetupBinding(DiscussionView editableTree, DocumentController controller, KeyController key, Context context)
        {
            ///editableTree.DataFieldKey = key;
            BindImageSource(editableTree, controller, key, context);
        }

        protected static void BindImageSource(DiscussionView editableTree, DocumentController docController, KeyController key, Context context)
        {
            var binding = new FieldBinding<ListController<FieldControllerBase>>
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.OneWay,
                Context = context,
                Converter = UriToBitmapImageConverter.Instance
            };
            //image.AddFieldBinding(Image.SourceProperty, binding);
        }
    }
}
