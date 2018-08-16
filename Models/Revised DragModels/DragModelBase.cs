using System.Collections.Generic;
using Windows.Foundation;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public abstract class DragModelBase
    {
        /*
         * These are the documents being dragged. Use GetDropDocuments() to get the documents to drop.
         */
        public List<DocumentController> DraggedDocuments;

        protected DragModelBase(List<DocumentController> draggedDocuments) => DraggedDocuments = draggedDocuments;

        public abstract List<DocumentController> GetDropDocuments(Point where, bool forceShowViewCopy = false);
    }
}