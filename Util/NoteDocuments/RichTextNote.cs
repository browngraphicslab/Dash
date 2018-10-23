using DashShared;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;

namespace Dash
{
    public class RichTextNote : NoteDocument
    {
        public static readonly DocumentType RichTextNoteDocumentType = new DocumentType("BC7128C2-E103-45AF-B3EC-38F979F7682D", "Rich Text Note");
        static string _prototypeID = "A79BB20B-A0D0-4F5C-81C6-95189AF0E90D";
        protected override DocumentType DocumentType => RichTextNoteDocumentType;

        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                //[KeyStore.DataKey]              = new RichTextController(new RichTextModel.RTD("Prototype Content")),
                [KeyStore.AbstractInterfaceKey] = new TextController("RichText Note Data API"),
                [KeyStore.OperatorKey] = new ListController<OperatorController>(new OperatorController[] { new RichTextDocumentOperatorController(), new RichTextTitleOperatorController() })
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Rich Text Data Prototype" };

            protoDoc.SetField(KeyStore.DocumentTextKey, new DocumentReferenceController(protoDoc, RichTextDocumentOperatorController.ReadableTextKey), true);
            protoDoc.SetField(KeyStore.TitleKey, new DocumentReferenceController(protoDoc, RichTextTitleOperatorController.ComputedTitle), true);
            return protoDoc;
        }

        static int rcount = 1;
        private RichTextView rtv;
        DocumentController CreateLayout(DocumentController dataDoc, Point where, Size size)
        {
            size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
            var rtb = new RichTextBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height).Document;
            return rtb;
        }

        public RichTextNote(string text = "Something to fill this space?", Point where = new Point(), Size size = new Size(), string urlSource=null) :
            base(_prototypeID)
        {
            var dataDocument = makeDataDelegate(new RichTextController(new RichTextModel.RTD(text + (urlSource != null ? RichTextView.HyperlinkText : ""))));
            Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument);
            Document.Tag = "Rich Text Note Layout " + rcount;
            dataDocument.Tag = "Rich Text Note Data" + rcount++;
            if (urlSource != null)
            {
                dataDocument.SetField<TextController>(KeyStore.SourceUriKey, urlSource, true);
                dataDocument.SetField<TextController>(KeyStore.WebContextKey, urlSource, true);
            }

        }
    }
}
