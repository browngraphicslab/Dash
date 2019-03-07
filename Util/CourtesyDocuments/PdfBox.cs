using System;
using System.Threading.Tasks;
using DashShared;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Dash
{
    class PdfBox : CourtesyDocument
    {
        /// <summary>
        /// The document type which is uniquely associated with pdf boxes
        /// </summary>
        public static DocumentType DocumentType = new DocumentType("16251F88-955D-49D0-BA87-DDF048EE2479", "Pdf Box");

        /// <summary>
        /// Default pdf uri is used by the prototype to supply a pdf uri if the user has not supplied one
        /// </summary>
        private static Uri DefaultPdfUri => null;

        /// <summary>
        /// The prototype id is used to make sure that only one prototype is every created
        /// </summary>
        private static string PrototypeId = "3150EE96-C106-4860-85F1-6724463FB29B";


        public PdfBox(FieldControllerBase refToPdf, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            // set fields based on the parameters
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToPdf);
            SetupDocument(DocumentType, PrototypeId, "PdfBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, KeyController key)
        {
            if (docController.GetDataDocument().GetField(KeyStore.PDFImageKey) != null)
            {
                return ImageBox.MakeView(docController, KeyStore.PDFImageKey);
            }
            var pdfView = new PdfView();
            SetupPdfBinding(pdfView, docController, key);
            return pdfView;
        }
        public static Task<DocumentController> MakeRegionDocument(DocumentView documentView, Point? point = null)
        {
            return documentView.GetFirstDescendantOfType<PdfView>().GetRegionDocument(point);
        }

        public static void SetupPdfBinding(PdfView pdf, DocumentController controller, KeyController key)
        {
            BindPdfSource(pdf, controller, key);
        }

        protected static void BindPdfSource(PdfView pdf, DocumentController docController, KeyController key)
        {
            var binding = new FieldBinding<PdfController>()
            {
                Document = docController,
                Key = key,
                Mode = BindingMode.TwoWay,
                //Converter = UriToStreamConverter.Instance
            };
            pdf.AddFieldBinding(PdfView.PdfUriProperty, binding);
        }
    }
}
