using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Windows.Foundation;
using Dash.Controllers;

namespace Dash
{
    class TemplateNote : NoteDocument
    {
        public static DocumentType DocumentType = new DocumentType("138AE495-4B1B-43EC-978D-6F91FBF3FCC7", "Template Note");
        static string _prototypeID = "24CE5031-7F29-4B12-9273-50D79B51CADB";

        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.AbstractInterfaceKey] = new TextController("Template Data API"),
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Template Editor Data Prototype" };

            return protoDoc;
        }

        static int rcount = 1;
        DocumentController CreateLayout(DocumentController workingDoc, Point @where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            return new TemplateEditorBox(workingDoc, where.X, where.Y, size.Width, size.Height).Document;
        }

        public TemplateNote(DocumentController workingDoc, Point where = new Point(), Size size = new Size()) :
            base(_prototypeID)
        {
            // data document's data key = list of layout documents
            var dataDocument =
                makeDataDelegate(new ListController<DocumentController>());
            Document = initSharedLayout(CreateLayout(workingDoc, where, size), dataDocument);
            // initSharedLayout sets data key to data document, we need to override it here
            Document.SetField(KeyStore.DataKey, workingDoc, true);
            //Document.SetField(KeyStore.TemplateDocumentKey, linkedToDoc.ViewModel.DataDocument, true);
            Document.Tag = "Template Data " + rcount;
            dataDocument.Tag = "Template Data" + rcount++;
        }
    }
}