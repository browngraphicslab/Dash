using DashShared;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    public class HtmlNote : NoteDocument
    {
        public static DocumentType DocumentType = new DocumentType("292C8EF7-D41D-49D6-8342-EC48AE014CBC", "Html Note");
        static string _prototypeID = "223BB098-78FA-4D61-8D18-D9E15086AC39";
        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.TitleKey] = new TextController("Prototype Title"),
                //  [KeyStore.DataKey] = new TextController("Prototype Content"),
                [KeyStore.DocumentTextKey] = new TextController("Prototype Html Text"),
                [KeyStore.AbstractInterfaceKey] = new TextController("Html Note Data API"),
                [KeyStore.OperatorKey] = new ListController<OperatorController>(new OperatorController[] { new RichTextTitleOperatorController() })
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Html Note Prototype" };

            protoDoc.SetField(KeyStore.TitleKey,
                new DocumentReferenceController(protoDoc, RichTextTitleOperatorController.ComputedTitle), true);

            return protoDoc;
        }

        DocumentController CreateLayout(DocumentController dataDocument, Point where, Size size)
        {
            size = new Size(size.Width == 0 ? 400 : size.Width, size.Height == 0 ? 400 : size.Height);
            return new WebBox(getDataReference(dataDocument), where.X, where.Y, size.Width, size.Height).Document;
        }

        static int hcount = 1;
        public HtmlNote(string text = "", string title = "", Point where = new Point(), Size size = new Size()) :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(new HtmlController(text ?? "Html stuff here"));
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument, title);
        }
        public HtmlNote(DocumentController dataDocument, Point where = new Point(), Size size = new Size()) :
            base(_prototypeID)
        {
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument);
            Document.Tag = "Html Note Layout " + hcount;
            dataDocument.Tag = "Html Note Data" + hcount++;
        }
    }
}
