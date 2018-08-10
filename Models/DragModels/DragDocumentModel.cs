using Dash.Controllers;
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

        public string LinkType = null;

        /// <summary>
        /// The XAML view that originated the drag operation
        /// </summary>
        public DocumentView LinkSourceView;

        /// <summary>
        ///     Drag the passed in document
        /// </summary>
        /// <param name="doc">the document to be dragged</param>
        /// <param name="showView">true to get a view copy, false to get the key value pane</param>
        public DragDocumentModel(DocumentController doc, bool showView, DocumentView sourceView = null)
        {
            DraggedDocument = doc;
            ShowViewCopy = showView;
            LinkSourceView = sourceView;
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
        /// Tests whether dropping the document would create a cycle and, if so, returns false
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public bool CanDrop(FrameworkElement sender)
        {
            if (sender is CollectionView cview && (MainPage.Instance.IsShiftPressed() || ShowViewCopy || MainPage.Instance.IsCtrlPressed()))
                return !cview.ViewModel.CreatesCycle(DraggedDocument);
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
                var dbox = new DataBox(new DocumentReferenceController(DraggedDocument, DraggedKey), where.X,
                    where.Y).Document;
                dbox.Tag = "DraggedKey doc";
                dbox.SetField(KeyStore.DocumentContextKey, DraggedDocument, true);
                //dbox.SetField(KeyStore.DataKey,
                //    new PointerReferenceController(new DocumentReferenceController(dbox.Id, KeyStore.DocumentContextKey), DraggedKey), true);
                dbox.SetTitle(DraggedKey.Name);
                return dbox;
            }

            // create a key value pane
            var ctrlState = MainPage.Instance.IsCtrlPressed();
            if (ctrlState) return DraggedDocument.GetDataInstance(where);
            
            var altState = MainPage.Instance.IsAltPressed();
            if (altState)
            {
                // create a key value pane
                return DraggedDocument.GetKeyValueAlias(where);
            }

            // create a view copy
            var vcopy = DraggedDocument.GetViewCopy(where);
            // when we drop a something that had no bounds (e.g., a workspace or a docked document), then we create
            // an arbitrary size for it and zero out its pan position so that it will FitToParent
            if (!vcopy.DocumentType.Equals(RichTextBox.DocumentType) &&
                double.IsNaN(vcopy.GetWidthField().Data) && double.IsNaN(vcopy.GetHeightField().Data))
            {
                vcopy.SetWidth(500);
                vcopy.SetHeight(300);
                vcopy.SetFitToParent(true);
            }
            return vcopy;
        }
    }
}