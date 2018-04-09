using System;
using Windows.UI.Xaml;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Dash.Converters;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class RichTextBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ED3B2D3C-C3EA-4FDC-9C0C-71E10F549C5F", "Rich Text Box");

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
                var binding = new FieldBinding<RichTextController>()
                {
                    Document = docController,
                    Key = KeyStore.DataKey,
                    Mode = BindingMode.TwoWay,
                    Context = context,
                    Tag = "Rich Text Box Text Binding"
                };
                element.AddFieldBinding(RichTextView.TextProperty, binding);

                var wrapBinding = new FieldBinding<TextController>()
                {
                    Document = docController,
                    Key = KeyStore.TextWrappingKey,
                    Mode = BindingMode.OneWay,
                    Converter = new StringToTextWrappingConverter(),
                    Context = context,
                    Tag = "Rich Text Box Text Wrapping Binding"
                };
                element.xRichEditBox.AddFieldBinding(RichEditBox.TextWrappingProperty, wrapBinding);
            }
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            RichTextView rtv = null;
            var dataField = docController.GetField(KeyStore.DataKey);
            var refToRichText = dataField as ReferenceController;
            var fieldModelController = (refToRichText?.DereferenceToRoot(context) ?? dataField) as RichTextController;
            if (fieldModelController != null)
            {
                rtv = new RichTextView()
                {
                    LayoutDocument = docController.GetActiveLayout() ?? docController,
                    DataDocument = refToRichText?.GetDocumentController(context) ?? docController.GetDataDocument()
                };
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