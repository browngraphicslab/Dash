using System;
using DashShared;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    public class AudioNote : NoteDocument
    {
        public static readonly DocumentType AudioDocumentType = new DocumentType("4C19B898-69D9-40A4-85B6-AD4AFFD5F679", "Audio Note");
        static string _prototypeID = "EDA4F2E9-690A-4FD5-A397-1A3ED68299FD";
        protected override DocumentType DocumentType => AudioDocumentType;

        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                //[KeyStore.DataKey]              = new RichTextController(new RichTextModel.RTD("Prototype Content")),
                [KeyStore.AbstractInterfaceKey] = new TextController("Audio Note Data API"),
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "AudioNote data prototype" };

            return protoDoc;
        }

        static int vcount = 1;
        DocumentController CreateLayout(DocumentController dataDoc, Point where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            return new AudioBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height).Document;
        }

        public AudioNote(Uri location, Point where = new Point(), Size size = new Size(), string title = "") :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(new AudioController(location));
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument, title);
            Document.Tag = "Audio Note Layout " + vcount;
            dataDocument.Tag = "Audio Note Data" + vcount++;
        }
    }
}
