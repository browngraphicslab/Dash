using DashShared;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;

namespace Dash
{
    public class DiscussionNote : NoteDocument
    {
        public static readonly DocumentType DiscussionNoteDocumentType = new DocumentType("DE65F2D1-C696-49CD-8EBA-5BB87B625F25", "Discussion Note");
        private static string _prototypeID = "06C73A36-25D1-494E-9DCB-ED46BBFBBB0F";
        protected override DocumentType DocumentType => DiscussionNoteDocumentType;

        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.AbstractInterfaceKey] = new TextController("Discussion Note Data API"),
             };
            return new DocumentController(fields, DocumentType, prototypeID) { Tag = "DiscussionNote Data Prototype" };
        }

        private DocumentController CreateLayout(DocumentController dataDoc, Point where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            return new DiscussionBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height).Document;
        }

        public DiscussionNote(string text = "Something to fill this space?", Point where = new Point(), Size size = new Size(), string urlSource = null) :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(new RichTextController(new RichTextModel.RTD(text)));
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument);
        }
    }
}
