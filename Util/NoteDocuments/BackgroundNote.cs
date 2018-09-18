using System;
using System.Collections.Generic;
using DashShared;
using Windows.Foundation;
using static Dash.BackgroundShape;
using Windows.UI;

namespace Dash
{
    public class BackgroundNote : NoteDocument
    {
        public static DocumentType DocumentType = new DocumentType("907DBC71-1078-48C4-8AD0-F388CACEEA0B", "Background Note");
        static string _prototypeID = "16133451-5E81-45E9-8A4D-1E7D9CF31036";
        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                //[KeyStore.DataKey]              = new TextController("adornment shape description"),
                [KeyStore.AbstractInterfaceKey] = new TextController("Background Note Data API"),
                //[KeyStore.TitleKey] = new TextController("Background Shape")
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Background data prototype" };

            return protoDoc;
        }

        static int bcount = 1;
        DocumentController CreateLayout(DocumentController dataDoc, Point where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            return new BackgroundShape(getDataReference(dataDoc), new DocumentReferenceController(dataDoc, KeyStore.SideCountKey), new DocumentReferenceController(dataDoc, KeyStore.BackgroundColorKey),  where.X, where.Y, size.Width, size.Height).Document;
        }
        public BackgroundNote(AdornmentShape shape, Point where = new Point(), Size size = new Size(), string title = "") :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(new TextController(shape.ToString()));
			
            var r = new Random();
            var hexColor = Color.FromArgb(0x33, (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));
            // set fields based on the parameters
            //TODO This should get set in background box/Why do BackgroundBox and BackgroundNote both need to exist?
            dataDocument.SetBackgroundColor(hexColor);
            dataDocument.SetSideCount(GroupGeometryConstants.DefaultCustomPolySideCount);
            dataDocument.SetTitle("Background : " + hexColor);

            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument, title);
            Document.Tag = "Background Note Layout " + bcount;
            dataDocument.Tag = "Background Note Data" + bcount++;
        }
    }
}
