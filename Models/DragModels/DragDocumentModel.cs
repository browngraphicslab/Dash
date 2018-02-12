using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Dash.Models.DragModels
{
    public class DragDocumentModel
    {
        DocumentController _documentController;
        KeyController      _documentKey = null;
        public DragDocumentModel(DocumentController doc, bool showView)
        {
            _documentController = doc;
            ShowViewModel = showView;
        }
        public DragDocumentModel(DocumentController doc, KeyController fieldKey)
        {
            _documentController = doc;
            _documentKey = fieldKey;
        }
        public bool               ShowViewModel = false;

        /// <summary>
        /// hack to stop people from dragging a collection on itself get the parent doc of the collection
        /// and then compare data docs, if they're equal then return
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public bool CanDrop(FrameworkElement sender)
        {
            var parentDocDataDoc = sender?.GetFirstAncestorOfType<CollectionView>()?
                .ParentDocument?.ViewModel?.DocumentController?.GetDataDocument();
            if (parentDocDataDoc != null && _documentController.GetDataDocument().Equals(parentDocDataDoc))
            {
                return false;
            }
            return true;
        }
        public KeyController DraggedKey { get => _documentKey;  }
        /// <summary>
        /// This is the document being dragged.  Use GetDropDocument() to get the document to drop.
        /// </summary>
        /// <returns></returns>
        public DocumentController GetDraggedDocument()
        {
            return _documentController;
        }
        public DocumentController GetDropDocument(Point where, bool forceLayoutDoc=false)
        {
            if (_documentKey != null)
            {
                var dbox = new DataBox(new DocumentReferenceController(_documentController.Id, _documentKey), where.X, where.Y).Document;
                dbox.SetField(KeyStore.DocumentContextKey, _documentController, true);
                dbox.SetField(KeyStore.TitleKey, new TextController(_documentKey.Name), true);
                return dbox;
            }
            else
            {
                var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift)
                    .HasFlag(CoreVirtualKeyStates.Down) || ShowViewModel || forceLayoutDoc;
                return shiftState ? _documentController.GetViewCopy(where) : _documentController.GetKeyValueAlias(where);
            }
        }
    }
}
