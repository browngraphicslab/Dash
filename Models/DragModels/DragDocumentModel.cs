using Windows.Foundation;
using Windows.UI.Xaml;

namespace Dash.Models.DragModels
{
    public class DragDocumentModel
    {
        /// <summary>
        ///     This is the document being dragged.  Use GetDropDocument() to get the document to drop.
        /// </summary>
        public DocumentController DraggedDocument { get; }

        /// <summary>
        ///     The key that is being dragged, can be null if an entire document is being dragged
        /// </summary>
        public KeyController DraggedKey { get; }

        /// <summary>
        /// True if the drag is going to result in a view copy, false if the drag will result in a key value pane
        /// </summary>
        public bool ShowViewCopy;


        /// <summary>
        ///     Drag the passed in document
        /// </summary>
        /// <param name="doc">the document to be dragged</param>
        /// <param name="showView">true to get a view copy, false to get the key value pane</param>
        public DragDocumentModel(DocumentController doc, bool showView)
        {
            DraggedDocument = doc;
            ShowViewCopy = showView;
        }

        /// <summary>
        ///     Drag the passed in document and key, will show a databox of the key's value
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="fieldKey"></param>
        public DragDocumentModel(DocumentController doc, KeyController fieldKey)
        {
            DraggedDocument = doc;
            DraggedKey = fieldKey;
        }

        /// <summary>
        ///     hack to stop people from dragging a collection on itself get the parent doc of the collection
        ///     and then compare data docs, if they're equal then return
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public bool CanDrop(FrameworkElement sender)
        {
            var parentDocDataDoc = ((sender as CollectionView) ?? sender?.GetFirstAncestorOfType<CollectionView>())?.ParentDocument?.ViewModel
                ?.DataDocument;
            if (parentDocDataDoc != null && DraggedDocument.GetDataDocument().Equals(parentDocDataDoc))
                return false;
            return true;
        }

        /// <summary>
        ///     Gets the document which will be dropped based on the current state of the syste
        /// </summary>
        public DocumentController GetDropDocument(Point where, bool forceShowViewCopy = false)
        {
            // if a key is specified use a databox to show the value stored at the key
            if (DraggedKey != null)
            {
                var dbox = new DataBox(new DocumentReferenceController(DraggedDocument.Id, DraggedKey), where.X,
                    where.Y).Document;
                dbox.SetField(KeyStore.DocumentContextKey, DraggedDocument, true);
                dbox.SetField(KeyStore.TitleKey, new TextController(DraggedKey.Name), true);
                return dbox;
            }


            // create an instance with the same view
            var ctrlState = MainPage.Instance.IsCtrlPressed();
            if (ctrlState) return DraggedDocument.GetDataInstance(where);

            // create a view copy
            var shiftState = MainPage.Instance.IsShiftPressed() || ShowViewCopy || forceShowViewCopy;
            if (shiftState)
            {
                var vcopy = DraggedDocument.GetViewCopy(where);
                // when we drop a collection that has no bounds (e.g., a workspace), then we create
                // an arbitrary size for it and zero out its pan position so that it will FitToParent
                if (double.IsNaN(vcopy.GetWidthField().Data) && double.IsNaN(vcopy.GetHeightField().Data) &&                    
                    vcopy.DocumentType.Equals(DashShared.DashConstants.TypeStore.CollectionBoxType))
                {
                    vcopy.SetField<NumberController>(KeyStore.WidthFieldKey, 500, true);
                    vcopy.SetField<NumberController>(KeyStore.HeightFieldKey,300, true);
                    vcopy.SetField<TextController>(KeyStore.CollectionFitToParentKey, "true", true);
                }
                return vcopy;
            }

            // create a key value pane
            return DraggedDocument.GetKeyValueAlias(where);
        }
    }
}