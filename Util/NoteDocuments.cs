using DashShared;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Dash.Controllers;

namespace Dash
{
    /// <summary>
    /// Class that makes document types that the user can take notes with 
    /// </summary>
    public static class NoteDocuments
    {
        public abstract class NoteDocument {
            public DocumentController Document { get; set; }
            
            public NoteDocument(string prototypeID)
            {
                _prototype = ContentController<FieldModel>.GetController<DocumentController>(prototypeID);
                if (_prototype == null)
                {
                    _prototype = createPrototype(prototypeID);
                }
            }
            protected DocumentController _prototype;
            protected abstract DocumentController createPrototype(string prototypeID);
            protected DocumentReferenceController getDataReference(string prototypeID)
            {
                return new DocumentReferenceController(prototypeID, KeyStore.DataKey);
            }
            protected DocumentController makeDataDelegate(FieldControllerBase controller)
            {
                var dataDocument = _prototype.MakeDelegate();
                dataDocument.SetField(KeyStore.DataKey, controller, true);
                return dataDocument;
            }

            protected DocumentController initSharedLayout(DocumentController layout, DocumentController dataDocument, string title = null)
            {
                if (!string.IsNullOrEmpty(title))
                    dataDocument.SetField(KeyStore.TitleKey, new TextController(title), true);
                layout.SetField(KeyStore.DocumentContextKey, dataDocument, true);
                //layout.SetField(KeyStore.DataKey,//TODO Get this to work
                //    new PointerReferenceController(
                //        new DocumentReferenceController(layout.Id, KeyStore.DocumentContextKey), KeyStore.DataKey), true);
                layout.SetField(KeyStore.DataKey,  new DocumentReferenceController(dataDocument.Id, KeyStore.DataKey), true);
                layout.SetField(KeyStore.TitleKey, new DocumentReferenceController(dataDocument.Id, KeyStore.TitleKey), true);
                return layout;
            }
        }
       

        public class CollectionNote : NoteDocument
        {
            public static DocumentType DocumentType = new DocumentType("EDDED871-DD89-4E6E-9C5E-A1CF927B3CB2", "Collected Docs Note");
            static string _prototypeID = "03F76CDF-21F1-404A-9B2C-3377C025DA0A";

            protected override DocumentController createPrototype(string prototypeID)
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>()
                {
                   // [KeyStore.DataKey] = new ListController<DocumentController>(),
                    [KeyStore.AbstractInterfaceKey] = new TextController("Collected Docs Note Data API"),
                    [KeyStore.OperatorKey] = new CollectionTitleOperatorController()
                };
                var protoDoc = new DocumentController(fields, DocumentType, prototypeID);

                protoDoc.SetField(KeyStore.TitleKey,
                    new DocumentReferenceController(protoDoc.Id, CollectionTitleOperatorController.ComputedTitle), true);

                protoDoc.Tag = "Collection Data Prototype";

                return protoDoc;
            }

            DocumentController CreateLayout(DocumentController dataDoc, CollectionView.CollectionViewType viewType, Point where, Size size)
            {
                return new CollectionBox(getDataReference(dataDoc.Id), where.X, where.Y, size.Width, size.Height, viewType).Document;
            }
            static int count = 1;
            public CollectionNote(Point where, CollectionView.CollectionViewType viewtype, double width=500, double height = 300, List<DocumentController> collectedDocuments = null) : 
                base(_prototypeID)
            {
                var dataDocument = makeDataDelegate(new ListController<DocumentController>());
                Document = initSharedLayout(CreateLayout(dataDocument, viewtype, where, new Size(width, height)), dataDocument);
                dataDocument.Tag = "Collection Note Data " + count;
                Document.Tag = "Collection Note Layout" + count++;

                // bcz : shouldn't need this, but something's up in the events that are sent to CollectionViewModel
                //Document.SetField(KeyStore.DataKey, new DocumentReferenceController(dataDocument.Id, KeyStore.DataKey), true);
                SetDocuments(collectedDocuments);
            }
            public void SetDocuments(List<DocumentController> collectedDocuments)
            {
                var listOfCollectedDocs = collectedDocuments ?? new List<DocumentController>();
                Document.GetDataDocument().SetField(KeyStore.DataKey, new ListController<DocumentController>(listOfCollectedDocs), true);

                if (listOfCollectedDocs?.Any() == true)
                {
                    Document.SetField(KeyStore.ThumbnailFieldKey, listOfCollectedDocs.FirstOrDefault(), true);
                    Document.SetField(KeyStore.CollectionFitToParentKey, new TextController("true"), true);
                }
            }
        }
        public class RichTextNote : NoteDocument
        {
            public static DocumentType DocumentType = new DocumentType("BC7128C2-E103-45AF-B3EC-38F979F7682D", "Rich Text Note");
            static string _prototypeID = "A79BB20B-A0D0-4F5C-81C6-95189AF0E90D";
            protected override DocumentController createPrototype(string prototypeID)
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>
                {
                    //[KeyStore.DataKey]              = new RichTextController(new RichTextModel.RTD("Prototype Content")),
                    [KeyStore.AbstractInterfaceKey] = new TextController("RichText Note Data API"),
                    [KeyStore.OperatorKey]          = new RichTextTitleOperatorController(),
                };
                var protoDoc = new DocumentController(fields, DocumentType, prototypeID);

                var docTextDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>
                {
                    [KeyStore.DataKey] = new DocumentReferenceController(protoDoc.Id, KeyStore.DataKey),
                    [KeyStore.OperatorKey] = new RichTextDocumentOperatorController(),
                }, DocumentType.DefaultType);
                protoDoc.SetField(KeyStore.DocumentTextKey,
                    new DocumentReferenceController(docTextDoc.Id, RichTextDocumentOperatorController.ReadableTextKey),
                    true);
                
                protoDoc.SetField(KeyStore.TitleKey,
                    new DocumentReferenceController(protoDoc.Id, RichTextTitleOperatorController.ComputedTitle), true);
                return protoDoc;
            }

            DocumentController CreateLayout(DocumentController dataDoc, Point where, Size size)
            {
                size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
                return new RichTextBox(getDataReference(dataDoc.Id), where.X, where.Y, size.Width, size.Height).Document;
            }
            
            public RichTextNote(string text = "Something to fill this space?", Point where = new Point(), Size size=new Size()) : 
                base(_prototypeID)
            {
                var dataDocument = makeDataDelegate(new RichTextController(new RichTextModel.RTD(text)));
                Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument);
                Document.SetField(KeyStore.TextWrappingKey, new TextController(!double.IsNaN(Document.GetWidthField().Data) ? DashShared.TextWrapping.Wrap.ToString() : DashShared.TextWrapping.NoWrap.ToString()), true);
            }
        }

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
                    [KeyStore.OperatorKey] = new RichTextTitleOperatorController()
                };
                var protoDoc = new DocumentController(fields, DocumentType, prototypeID);

                protoDoc.SetField(KeyStore.TitleKey,
                    new DocumentReferenceController(protoDoc.Id, RichTextTitleOperatorController.ComputedTitle), true);

                return protoDoc;
            }
            
            DocumentController CreateLayout(DocumentController dataDocument, Point where, Size size)
            {
                return new WebBox(getDataReference(dataDocument.Id), where.X, where.Y, size.Width == 0 ? 400 : size.Width, size.Height == 0 ? 400 : size.Height).Document;
            }
            
            public HtmlNote(string text = "", string title = "", Point where = new Point(), Size size = new Size()) : 
                base(_prototypeID)
            {
                var dataDocument = makeDataDelegate(new TextController(text ?? "Html stuff here"));
                Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument, title);
            }
            public HtmlNote(DocumentController dataDocument, Point where = new Point(), Size size = new Size()) :
               base(_prototypeID)
            {
                Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument);
            }
        }
        public class PostitNote : NoteDocument
        {
            public static DocumentType DocumentType = new DocumentType("4C20B539-BF40-4B60-9FA4-2CC531D3C757", "Text Note");
            static string _prototypeID = "08AC0453-D39F-45E3-81D9-C240B7283BCA";
            protected override DocumentController createPrototype(string prototypeID)
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>
                {
                    [KeyStore.TitleKey]             = new TextController("Prototype Title"),
                  //  [KeyStore.DataKey]              = new TextController("Prototype Content"),
                    [KeyStore.AbstractInterfaceKey] = new TextController("PostIt Note Data API"),
                };
                return new DocumentController(fields, DocumentType, prototypeID);
            }

            DocumentController CreateLayout(DocumentController dataDocument, Point where, Size size)
            {
                return new TextingBox(getDataReference(dataDocument.Id), where.X, where.Y, size.Width, size.Height).Document;
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
}
