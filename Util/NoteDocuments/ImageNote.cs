using System;
using DashShared;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    public class ImageNote : NoteDocument
    {
        public static readonly DocumentType ImageDocumentType = new DocumentType("80577E19-5AE6-4BEF-940C-E516CE154684", "Image Note");
        static string _prototypeID = "36AF28B6-5EEF-48E2-9C4E-3698C77AE005";
        protected override DocumentType DocumentType => ImageDocumentType;

        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                //[KeyStore.DataKey]              = new RichTextController(new RichTextModel.RTD("Prototype Content")),
                [KeyStore.AbstractInterfaceKey] = new TextController("Image Note Data API"),
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "ImageNote data prototype" };

            return protoDoc;
        }

        static int icount = 1;
        DocumentController CreateLayout(DocumentController dataDoc, Point where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            //image box is created from imagenote
            return new ImageBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height).Document;
        }

	    public ImageNote(Uri location, Point where = new Point(), Size size = new Size(), string title = "") :
            base(_prototypeID)
        {
            //location is the URI we want
            var dataDocument = makeDataDelegate(new ImageController(location));
            
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument, title);
            Document.Tag = "Image Note Layout " + icount;
            dataDocument.Tag = "Image Note Data" + icount++;
        }
    }
}
