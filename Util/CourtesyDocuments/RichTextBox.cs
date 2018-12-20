using System;
using Windows.UI.Xaml;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dash
{
    public class RichTextBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ED3B2D3C-C3EA-4FDC-9C0C-71E10F549C5F", "Rich Text Box");
        private static readonly string PrototypeId = "001EDE6C-A713-4C54-BF98-0BAFB6230D61";

        public RichTextBox(FieldControllerBase refToRichText, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToRichText);
            SetupDocument(DocumentType, PrototypeId, "RichTextBox Prototype Layout", fields);
        }
        public class TextWrappingConverter : SafeDataToXamlConverter<double, TextWrapping>
        {
            public override TextWrapping ConvertDataToXaml(double wrapping, object parameter = null)
            {
                return (TextWrapping)(int)wrapping;
            }

            public override double ConvertXamlToData(Windows.UI.Xaml.TextWrapping xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }
        public static void SetupBindings(RichEditView element, DocumentController docController, KeyController key)
        {
            element.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Black);
            element.DataFieldKey = key;
            var binding = new FieldBinding<RichTextController>()
            {
                Document = docController,
                Key = key,
                Mode = BindingMode.TwoWay,
                Tag = "Rich Text Box Text Binding",
                FallbackValue = new RichTextModel.RTD() {RtfFormatString="" }
            };
            element.AddFieldBinding(RichEditView.TextProperty, binding);
            SetupTextWrapBinding(element, docController);
        }

        public static void SetupTextWrapBinding(RichEditView element, DocumentController docController)
        {
            var twrapBinding = new FieldBinding<NumberController>
            {
                Document = docController,
                Key = KeyStore.TextWrappingKey,
                Mode = BindingMode.OneWay,
                Converter = new TextWrappingConverter(),
                Tag = "Rich Text Box Text Wrapping Binding",
                FallbackValue = TextWrapping.Wrap
            };
            element.AddFieldBinding(RichEditBox.TextWrappingProperty, twrapBinding);
        }
        public static Task<DocumentController> MakeRegionDocument(DocumentView richTextBox, Point? point = null)
        {
            var rtv = richTextBox.GetFirstDescendantOfType<RichEditView>();
            return Task.FromResult(rtv?.GetRegionDocument());
        }
        
        public static FrameworkElement MakeView(DocumentController docController, KeyController key)
        {
            RichEditView rtv = null;
            var dataField = docController.GetField(key);
            var refToRichText = dataField as ReferenceController;
            rtv = new RichEditView() { FontSize = SettingsView.Instance.NoteFontSize };
            //{
            //    LayoutDocument = docController,
            //    // bcz: need to work on this ... somehow we want to guarantee that we're getting a DataDocument, but GetDataDocument() isn't recursive in the case that it has a LayoutDocument
            //    DataDocument = refToRichText?.GetDocumentController(context).GetDataDocument() ?? docController.GetDataDocument()
            //};
            rtv.ManipulationMode = ManipulationModes.All;
            rtv.PointerEntered += (sender, args) => rtv.ManipulationMode = ManipulationModes.None;
            rtv.GotFocus       += (sender, args) => rtv.ManipulationMode = ManipulationModes.None;
            rtv.LostFocus      += (sender, args) => rtv.ManipulationMode = ManipulationModes.All;
            //TODO: lose focus when you drag the rich text view so that text doesn't select at the same time
            rtv.HorizontalAlignment = HorizontalAlignment.Stretch;
            rtv.VerticalAlignment = VerticalAlignment.Stretch;
            SetupBindings(rtv, docController, key);
            return rtv;
        }

        private static ReferenceController GetTextReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
        }
    }

}
