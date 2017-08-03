using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Dash;
using DashShared;

namespace Dash
{
    public class RichTextBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ED3B2D3C-C3EA-4FDC-9C0C-71E10F549C5F", "Rich Text");

        public RichTextBox(FieldModelController refToRichText, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(x, y, w, h, refToRichText);
            Document = new DocumentController(fields, DocumentType);
            SetLayoutForDocument(Document, Document, forceMask: true, addToLayoutList: true);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            RichTextView rtv = null;
            var refToRichText =
                docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
            Debug.Assert(refToRichText != null);
            var fieldModelController = refToRichText.DereferenceToRoot(context);
            if (fieldModelController is RichTextFieldModelController)
            {
                var richTextFieldModelController = fieldModelController as RichTextFieldModelController;
                Debug.Assert(richTextFieldModelController != null);
                var richText = new RichTextView(richTextFieldModelController, refToRichText, context);
                rtv = richText;
                rtv.HorizontalAlignment = HorizontalAlignment.Stretch;
                rtv.VerticalAlignment = VerticalAlignment.Stretch;
            }

            // bind the rich text height
            var heightController = GetHeightField(docController, context);
            BindHeight(rtv, heightController);

            // bind the rich text width
            var widthController = GetWidthField(docController, context);
            BindWidth(rtv, widthController);

            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(rtv, docController);
            }
            return rtv;
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }
    }

}