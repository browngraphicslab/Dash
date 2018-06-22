using DashShared;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    public class TemplateEditorBase : NoteDocument
    {
        public static DocumentType DocumentType = new DocumentType("0AD8E8E2-D414-4AC3-9D33-98BA185510A2", "Template Editor Base");
        static string _prototypeID = "804CB4BF-AB4D-4600-AD92-3AD31AFFA10B";
        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.AbstractInterfaceKey] = new TextController("TemplateEditor Base Data API"),
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Template Editor Data Prototype" };

            protoDoc.SetField(KeyStore.DocumentTextKey, new DocumentReferenceController(protoDoc.Id, RichTextDocumentOperatorController.ReadableTextKey), true);
            protoDoc.SetField(KeyStore.TitleKey, new DocumentReferenceController(protoDoc.Id, RichTextTitleOperatorController.ComputedTitle), true);
            return protoDoc;
        }

        static int rcount = 1;
        DocumentController CreateLayout(DocumentController dataDoc, Point @where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            return new TemplateEditorBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height).Document;
        }

        public TemplateEditorBase(DocumentView linkedToDoc, DocumentController doc, Point where = new Point(), Size size = new Size()) :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(doc);
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument);
            Document.SetField(KeyStore.TemplateDocumentKey, linkedToDoc.ViewModel.DataDocument, true);
            Document.Tag = "Template Editor Data " + rcount;
            dataDocument.Tag = "Template Editor Data" + rcount++;
        }
    }
}
