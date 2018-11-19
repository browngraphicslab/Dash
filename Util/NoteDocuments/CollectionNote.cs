using System.Collections.Generic;
using DashShared;
using System.Linq;
using Windows.Foundation;
using System;

namespace Dash
{
    public class CollectionNote : NoteDocument
    {
        public static readonly DocumentType CollectionNoteDocumentType = new DocumentType("EDDED871-DD89-4E6E-9C5E-A1CF927B3CB2", "Collected Docs Note");
        static string _prototypeID = "03F76CDF-21F1-404A-9B2C-3377C025DA0A";
        protected override DocumentType DocumentType => CollectionNoteDocumentType;

        protected override DocumentController createPrototype(string prototypeID)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>()
            {
                // [KeyStore.DataKey] = new ListController<DocumentController>(),
                [KeyStore.AbstractInterfaceKey] = new TextController("Collected Docs Note Data API"),
                [KeyStore.OperatorKey] = new ListController<OperatorController>(new OperatorController[] { new CollectionTitleOperatorController() }),
                [KeyStore.FolderIconKey] = new ImageController(new Uri("https://image.freepik.com/free-vector/illustration-of-data-folder-icon_53876-6329.jpg"))
            };
            return new DocumentController(fields, DocumentType, prototypeID) { Tag = "CollectionNote Data Prototype" };
        }

        DocumentController CreateLayout(DocumentController dataDoc, CollectionViewType viewType, Point where, Size size)
        {
            return new CollectionBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height, viewType).Document;
        }
        static int count = 1;
        public CollectionNote(Point where, CollectionViewType viewtype, double width = 500, double height = 300, IEnumerable<DocumentController> collectedDocuments = null) :
            base(_prototypeID)
        {
            DocumentController dataDocument = makeDataDelegate(new ListController<DocumentController>());
            Document = initSharedLayout(CreateLayout(dataDocument, viewtype, where, new Size(width, height)), dataDocument);
            dataDocument.Tag = "Collection Note Data " + count;
            Document.Tag = "Collection Note Layout" + count++;

            if (viewtype == CollectionViewType.Icon)
            {
                Document.SetField<TextController>(KeyStore.CollectionOpenViewTypeKey, CollectionViewType.Freeform.ToString(), true);
            }

            dataDocument.SetField(KeyStore.InkDataKey, new InkController(), true);
            if (double.IsNaN(width) && double.IsNaN(height))
            {
                Document.SetHorizontalAlignment(Windows.UI.Xaml.HorizontalAlignment.Stretch);
                Document.SetVerticalAlignment(Windows.UI.Xaml.VerticalAlignment.Stretch);
            }

            // bcz : shouldn't need this, but something's up in the events that are sent to CollectionViewModel
            //Document.SetField(KeyStore.DataKey, new DocumentReferenceController(dataDocument.Id, KeyStore.DataKey), true);

            //TODO tfs: this shouldn't need to be called, we should be able to pass collectedDocuments into makeDataDelegate

            SetDocuments(collectedDocuments);
        }
        public void SetDocuments(IEnumerable<DocumentController> collectedDocuments)
        {
            var listOfCollectedDocs = collectedDocuments?.ToList() ?? new List<DocumentController>();
            Document.GetDataDocument().SetField(KeyStore.DataKey, new ListController<DocumentController>(listOfCollectedDocs), true);

            if (listOfCollectedDocs?.Any() == true)
            {
                Document.SetField(KeyStore.ThumbnailFieldKey, listOfCollectedDocs.FirstOrDefault(), true);
                Document.SetFitToParent(true);
            }
        }
    }
}
