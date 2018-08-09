using System;
using DashShared;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Syncfusion.Windows.PdfViewer;
using System.Linq;
using Dash.Views;

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

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // create the pdf view
            //var pdfView = new PdfView() { DataContext = docController,
            //    LayoutDocument = docController.GetActiveLayout() ?? docController,
            //    DataDocument = docController.GetDataDocument()
            //};
            //var pdf = pdfView.Pdf;

            //// make the pdf respond to resizing, interactions etc...
            //SetupBindings(pdfView, docController, context);
            //SetupPdfBinding(pdf, docController, context);

            MainPage.Instance.TogglePopup();
            var pdfView = new CustomPdfView(docController);
            SetupBindings(pdfView, docController, context);
            SetupPdfBinding(pdfView, docController, context);
            
            return pdfView;
        }

        private static ReferenceController GetPdfReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
        }

        public static DocumentController MakeRegionDocument(DocumentView documentView, Point? point = null)
        {
            var pdf = documentView.GetFirstDescendantOfType<CustomPdfView>();
            return pdf.GetRegionDocument(point);
        }
        protected static void SetupPdfBinding(CustomPdfView pdf, DocumentController controller,
            Context context)
        {

            //controller.AddFieldUpdatedListener(KeyStore.PdfVOffsetFieldKey, 
            //    delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
            //    {
            //        if (!pdf.IsPointerOver())
            //        {
            //            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)args;
            //            System.Diagnostics.Debug.WriteLine("Telling me" + ((dargs.NewValue as NumberController)?.Data ?? 0));
            //            pdf.ScrollToVerticalOffset((dargs.NewValue as NumberController)?.Data ?? 0);
            //        }
            //    });


            BindPdfSource(pdf, controller, context);
        }

        protected static void BindPdfSource(CustomPdfView pdf, DocumentController docController, Context context)
        {
            var binding = new FieldBinding<ImageController>()
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.TwoWay,
                Context = context,
                //Converter = UriToStreamConverter.Instance
            };
            pdf.AddFieldBinding(CustomPdfView.PdfUriProperty, binding);
        }
    }
}
