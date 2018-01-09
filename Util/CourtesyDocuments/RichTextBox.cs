using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Dash;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    public class RichTextBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ED3B2D3C-C3EA-4FDC-9C0C-71E10F549C5F", "Rich Text");

        public RichTextBox(FieldControllerBase refToRichText, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x,y), new Size(w,h), refToRichText);
            Document = new DocumentController(fields, DocumentType);
        }
        protected static void SetupTextBinding(RichTextView element, DocumentController docController, Context context)
        {
            var data = docController.GetDereferencedField(KeyStore.DataKey, context);
            if (data != null)
            {
                var binding = new FieldBinding<FieldControllerBase>()
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
            Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null, bool isInterfaceBuilderLayout = false)
        {
            RichTextView rtv = null;
            var refToRichText =
                docController.GetField(KeyStore.DataKey) as ReferenceController;
            Debug.Assert(refToRichText != null);
            var fieldModelController = refToRichText.DereferenceToRoot(context);
            var referenceToText = GetTextReference(docController);
            if (fieldModelController is RichTextController)
            {

                var richText = new RichTextView()
                {
                    TargetFieldReference = referenceToText,
                    TargetDocContext = context,
                    DataDocument = refToRichText.GetDocumentController(context)
                    
                };
                rtv = richText;
                rtv.ManipulationMode = ManipulationModes.All;
                rtv.PointerEntered += (sender, args) => rtv.ManipulationMode = ManipulationModes.None;
                rtv.GotFocus += (sender, args) => rtv.ManipulationMode = ManipulationModes.None;
                rtv.LostFocus += (sender, args) => rtv.ManipulationMode = ManipulationModes.All;
                //TODO: lose focus when you drag the rich text view so that text doesn't select at the same time
                rtv.HorizontalAlignment = HorizontalAlignment.Stretch;
                rtv.VerticalAlignment = VerticalAlignment.Stretch;
            }
            SetupTextBinding(rtv, docController, context);
            SetupBindings(rtv, docController, context);


            //add to key to framework element dictionary
            var reference = docController.GetField(KeyStore.DataKey) as ReferenceController;
            if (keysToFrameworkElementsIn != null) keysToFrameworkElementsIn[reference?.FieldKey] = rtv;

            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(rtv, docController);
            }
            return rtv;
        }

        private static ReferenceController GetTextReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
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