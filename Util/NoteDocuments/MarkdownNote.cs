using DashShared;
using System.Collections.Generic;
using Windows.Foundation;

namespace Dash
{
    class MarkdownNote : NoteDocument 
    {
        public static DocumentType DocumentType = new DocumentType("4F6E6BA8-E18B-4CE2-A575-105A33017328", "Markdown Note");
        static string _prototypeID = "5296EA59-C0EB-4853-822B-D7BD426A316E";
        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
              //  [KeyStore.DataKey]              = new RichTextController(new RichTextModel.RTD("Prototype Content")),
                [KeyStore.AbstractInterfaceKey] = new TextController("Markdown Note Data API"),
               // [KeyStore.OperatorKey] = new ListController<OperatorController>(new OperatorController[] { new MarkdownDocumentOperatorController(), new MarkdownTitleOperatorController() })
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Markdown Data Prototype" };

        //    protoDoc.SetField(KeyStore.DocumentTextKey, new DocumentReferenceController(protoDoc.Id, MarkdownDocumentOperatorController.ReadableTextKey), true);
         //   protoDoc.SetField(KeyStore.TitleKey, new DocumentReferenceController(protoDoc.Id, MarkdownTitleOperatorController.ComputedTitle), true);
            return protoDoc;
        }

        static int rcount = 1;
        DocumentController CreateLayout(DocumentController dataDoc, Point where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            return new MarkdownBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height).Document;
        }

        public MarkdownNote(string text = "Something to fill this space?", Point where = new Point(), Size size = new Size()) :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(new RichTextController(new RichTextModel.RTD(text)));
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument);
            Document.Tag = "Markdown Note Layout " + rcount;
            dataDocument.Tag = "Markdown Note Data" + rcount++;
            Document.SetField(KeyStore.TextWrappingKey, new TextController(!double.IsNaN(Document.GetWidthField().Data) ? DashShared.TextWrapping.Wrap.ToString() : DashShared.TextWrapping.NoWrap.ToString()), true);
        }
    }
}
