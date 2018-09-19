using System;
using Windows.UI.Xaml;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Dash
{
    public class RichTextBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ED3B2D3C-C3EA-4FDC-9C0C-71E10F549C5F", "Rich Text Box");
        private static readonly string PrototypeId = "001EDE6C-A713-4C54-BF98-0BAFB6230D61";

        public RichTextBox(FieldControllerBase refToRichText, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x,y), new Size(w,h), refToRichText);
            SetupDocument(DocumentType, PrototypeId, "RichTextBox Prototype Layout", fields);
        }
        public class AutomatedTextWrappingBinding : SafeDataToXamlConverter<System.Collections.Generic.List<object>, Windows.UI.Xaml.TextWrapping>
        {
            public override Windows.UI.Xaml.TextWrapping ConvertDataToXaml(List<object> data, object parameter = null)
            {
                if (data[0] != null && data[0] is double)
                {
                    switch ((int)(double)data[0])
                    {
                        case (int)Windows.UI.Xaml.TextWrapping.Wrap:
                            return Windows.UI.Xaml.TextWrapping.Wrap;
                        case (int)Windows.UI.Xaml.TextWrapping.NoWrap:
                            return Windows.UI.Xaml.TextWrapping.NoWrap;
                    }
                }
                double width = (double)data[1];
                return double.IsNaN(width) ? Windows.UI.Xaml.TextWrapping.NoWrap : Windows.UI.Xaml.TextWrapping.Wrap;
            }

            public override List<object> ConvertXamlToData(Windows.UI.Xaml.TextWrapping xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
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

                var textWrapRef = new DocumentFieldReference(docController, KeyStore.TextWrappingKey);
                var widthRef = new DocumentFieldReference(docController, KeyStore.WidthFieldKey);
                var twrapBinding = new FieldMultiBinding<Windows.UI.Xaml.TextWrapping>(textWrapRef, widthRef)
                {
                    Mode = BindingMode.OneWay,
                    Converter = new AutomatedTextWrappingBinding(),
                    Context = context,
                    Tag = "Rich Text Box Text Wrapping Binding",
                    CanBeNull = true
                };
                element.xRichEditBox.AddFieldBinding(RichEditBox.TextWrappingProperty, twrapBinding);
            }
        }
        public static DocumentController MakeRegionDocument(DocumentView richTextBox, Point? point = null)
        {
            var rtv = richTextBox.GetFirstDescendantOfType<RichTextView>();
            return rtv.GetRegionDocument();
        }
        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            RichTextView rtv = null;
            var dataField = docController.GetField(KeyStore.DataKey);
            var refToRichText = dataField as ReferenceController;
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
                SetupTextBinding(rtv, docController, context);
                SetupBindings(rtv, docController, context);
			
            return rtv;
        }

        private static ReferenceController GetTextReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
        }
    }

}
