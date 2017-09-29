using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Dash;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class RichTextBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ED3B2D3C-C3EA-4FDC-9C0C-71E10F549C5F", "Rich Text");

        public RichTextBox(FieldModelController refToRichText, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(x, y, w, h, refToRichText);
            Document = new DocumentController(fields, DocumentType);
        }
        protected static void SetupTextBinding(RichTextView element, DocumentController docController, Context context)
        {
            var data = docController.GetDereferencedField(KeyStore.DataKey, context);
            if (data != null)
            {
                var binding = new FieldBinding<FieldModelController>()
                {
                    Document = docController,
                    Key = KeyStore.DataKey,
                    Mode = BindingMode.TwoWay,
                    Context = context
                };
                element.AddFieldBinding(RichTextView.TextProperty, binding);
            }
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            RichTextView rtv = null;
            var refToRichText =
                docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
            Debug.Assert(refToRichText != null);
            var fieldModelController = refToRichText.DereferenceToRoot(context);
            var referenceToText = GetTextReference(docController);
            if (fieldModelController is RichTextFieldModelController)
            {

                var richText = new RichTextView()
                {
                    TargetFieldReference = referenceToText,
                    TargetDocContext = context
                    
                };
                rtv = richText;
                rtv.GotFocus += (sender, args) => rtv.ManipulationMode = ManipulationModes.None;
                rtv.LostFocus += (sender, args) => rtv.ManipulationMode = ManipulationModes.All;
                //TODO: lose focus when you drag the rich text view so that text doesn't select at the same time
                rtv.HorizontalAlignment = HorizontalAlignment.Stretch;
                rtv.VerticalAlignment = VerticalAlignment.Stretch;
            }
            SetupTextBinding(rtv, docController, context);

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

        private static ReferenceFieldModelController GetTextReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
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