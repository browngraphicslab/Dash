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
        public KeyController DraggedKey { get; private set; }

        public JoinDragModel(List<DocumentController> draggedDocs, KeyController draggedKey)
        {
            DraggedDocuments = draggedDocs;
            DraggedKey = draggedKey;
        }
    }
}
