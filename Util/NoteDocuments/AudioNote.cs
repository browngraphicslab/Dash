using System;
using DashShared;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    public class AudioNote : NoteDocument
    {
        public static DocumentType DocumentType = new DocumentType("E9D1BEAF-8D88-4C00-958B-A1C7DB3AB560", "Audio Note");
        static string _prototypeID = "9D2573C1-1FA2-49ED-9C38-425224D9F685";
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
            var dataDocument = makeDataDelegate(new ImageController(location));
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument, title);
            Document.Tag = "Audio Note Layout " + vcount;
            dataDocument.Tag = "Audio Note Data" + vcount++;
        }
    }
}
