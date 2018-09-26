using System.Collections.Generic;
using DashShared;
using System.Linq;
using Windows.Foundation;

namespace Dash
{
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
                [KeyStore.OperatorKey] = new ListController<OperatorController>(new OperatorController[] { new CollectionTitleOperatorController() })
            };
            var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "CollectionNote Data Prototype" };

            protoDoc.SetField(KeyStore.TitleKey,
                new DocumentReferenceController(protoDoc, CollectionTitleOperatorController.ComputedTitle), true);

            return protoDoc;
        }

        DocumentController CreateLayout(DocumentController dataDoc, CollectionView.CollectionViewType viewType, Point where, Size size)
        {
            return new CollectionBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height, viewType).Document;
        }
        static int count = 1;
        public CollectionNote(Point where, CollectionView.CollectionViewType viewtype, double width = 500, double height = 300, List<DocumentController> collectedDocuments = null) :
            base(_prototypeID)
        {
            DocumentController dataDocument = makeDataDelegate(new ListController<DocumentController>());
            Document = initSharedLayout(CreateLayout(dataDocument, viewtype, where, new Size(width, height)), dataDocument);
            dataDocument.Tag = "Collection Note Data " + count;
            Document.Tag = "Collection Note Layout" + count++;

            dataDocument.SetField(KeyStore.InkDataKey, new InkController(), true);

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
                Document.SetFitToParent(true);
            }
        }
    }
}
