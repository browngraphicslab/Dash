using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class JoinDragModel
    {
        public List<DocumentController> DraggedDocuments { get; private set; }
        public DocumentController CollectionDocument { get; private set; }
        public KeyController DraggedKey { get; private set; }

        public JoinDragModel(DocumentController collectionDocument, List<DocumentController> draggedDocs, KeyController draggedKey)
        {
            CollectionDocument = collectionDocument;
            DraggedDocuments = draggedDocs;
            DraggedKey = draggedKey;
        }
    }
}
