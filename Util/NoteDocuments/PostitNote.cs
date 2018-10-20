using System.Collections.Generic;
using DashShared;
using Windows.Foundation;

namespace Dash
{
    public class PostitNote : NoteDocument
    {
        public static readonly DocumentType PostitNoteDocumentType = new DocumentType("4C20B539-BF40-4B60-9FA4-2CC531D3C757", "Text Note");
        static string _prototypeID = "08AC0453-D39F-45E3-81D9-C240B7283BCA";
        protected override DocumentType DocumentType => PostitNoteDocumentType;

        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.TitleKey] = new TextController("Prototype Title"),
                //  [KeyStore.DataKey]              = new TextController("Prototype Content"),
                [KeyStore.AbstractInterfaceKey] = new TextController("PostIt Note Data API"),
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Postit Note Protoype" };
            protoDoc.SetField(KeyStore.DocumentTextKey, new DocumentReferenceController(protoDoc, KeyStore.DataKey), true);
            return protoDoc;
        }

        DocumentController CreateLayout(DocumentController dataDocument, Point where, Size size)
        {
            return new TextingBox(getDataReference(dataDocument), where.X, where.Y, size.Width, size.Height).Document;
        }

        // TODO for bcz - takes in text and title to display, docType is by default the one stored in this class
        public PostitNote(string text = null, string title = null, DocumentType type = null, Point where = new Point(), Size size = new Size()) :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(new TextController(text ?? "Write something amazing!"));
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument, title);
        }
    }
}
