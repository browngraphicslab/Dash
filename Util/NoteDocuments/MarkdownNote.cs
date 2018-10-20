using DashShared;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    class MarkdownNote : NoteDocument 
    {
        public static readonly DocumentType MarkdownDocumentType = new DocumentType("4F6E6BA8-E18B-4CE2-A575-105A33017328", "Markdown Note");
        static string _prototypeID = "5296EA59-C0EB-4853-822B-D7BD426A316E";
        protected override DocumentType DocumentType => MarkdownDocumentType;

        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.TitleKey] = new TextController("Prototype Title"),
                //  [KeyStore.DataKey]              = new TextController("Prototype Content"),
                [KeyStore.AbstractInterfaceKey] = new TextController("Markdown Note Data API"),
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Markdown Note Protoype" };
            protoDoc.SetField(KeyStore.DocumentTextKey, new DocumentReferenceController(protoDoc, KeyStore.DataKey), true);
            return protoDoc;
        }
        
        DocumentController CreateLayout(DocumentController dataDoc, Point where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            return new MarkdownBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height).Document;
        }

        static int rcount = 1;
        public MarkdownNote(string text = "Write something amazing!", string title = null, DocumentType type = null, Point where = new Point(), Size size = new Size()) :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(new TextController(text));
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument, title);
            
            Document.Tag = "Markdown Note Layout " + rcount;
            dataDocument.Tag = "Markdown Note Data " + rcount++;
        }
    }
}
