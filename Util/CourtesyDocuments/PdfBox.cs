﻿using System;
using DashShared;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Syncfusion.Windows.PdfViewer;
using System.Linq;

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

            //// update the horizontal and vertical alignment fields to be separate from teh default
            //if (fields[KeyStore.HorizontalAlignmentKey] is TextController horzTextController)
            //    horzTextController.Data = HorizontalAlignment.Left.ToString();
            //if (fields[KeyStore.VerticalAlignmentKey] is TextController vertTextController)
            //    vertTextController.Data = VerticalAlignment.Top.ToString();

            // get a delegate of the prototype layout (which already has fields set on it)
            Document = GetLayoutPrototype().MakeDelegate();

            // replace any of the default fields on the prototype delegate with the new fields
            Document.SetFields(fields, true);
            Document.SetField(KeyStore.DocumentContextKey, GetPdfReference(Document).GetDocumentController(null), true);
        }

        /// <summary>
        /// Returns the prototype layout if it exists, otherwise creates a new prototype layout
        /// </summary>
        /// <returns></returns>
        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<FieldModel>.GetController<DocumentController>(PrototypeId) ??
                            InstantiatePrototypeLayout();
            return prototype;
        }

        /// <summary>
        /// Creates a new prototype layout
        /// </summary>
        /// <returns></returns>
        protected override DocumentController InstantiatePrototypeLayout()
        {
            var pdfController = new ImageController(DefaultPdfUri);
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), pdfController);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }

        public override FrameworkElement makeView(DocumentController docController, Context context)
        {
            return MakeView(docController, context);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // create the pdf view
            var pdfView = new PdfView() { DataContext = docController };
            var pdf = pdfView.Pdf;

            // make the pdf respond to resizing, interactions etc...
            SetupBindings(pdfView, docController, context);
            SetupPdfBinding(pdf, docController, context);
            
            return pdfView;
        }

        private static ReferenceController GetPdfReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
        }

        protected static void SetupPdfBinding(SfPdfViewerControl pdf, DocumentController controller,
            Context context)
        {
            var reference = GetPdfReference(controller);
            var dataDoc = reference.GetDocumentController(context);
            dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                {
                    var doc = (DocumentController)sender;
                    var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)args;
                    if (args.Action != DocumentController.FieldUpdatedAction.Update && !dargs.FromDelegate)
                    {
                        BindPdfSource(pdf, doc, c, reference.FieldKey);
                    }
                });

            controller.AddFieldUpdatedListener(KeyStore.PdfVOffsetFieldKey, 
                delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                {
                    if (!pdf.IsPointerOver())
                    {
                        var dargs = (DocumentController.DocumentFieldUpdatedEventArgs)args;
                        System.Diagnostics.Debug.WriteLine("Telling me" + ((dargs.NewValue as NumberController)?.Data ?? 0));
                        pdf.ScrollToVerticalOffset((dargs.NewValue as NumberController)?.Data ?? 0);
                    }
                });


            BindPdfSource(pdf, controller, context, KeyStore.DataKey);
        }

        protected static void BindPdfSource(SfPdfViewerControl pdf, DocumentController docController, Context context, KeyController key)
        {
            var data = docController.GetDereferencedField(key, context) as ImageController;
            if (data == null)
            {
                return;
            }
            var binding = new FieldBinding<ImageController>()
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.TwoWay,
                Context = context,
                Converter = UriToStreamConverter.Instance
            };
            pdf.AddFieldBinding(SfPdfViewerControl.ItemsSourceProperty, binding);
        }
    }
}
