using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public abstract class DragModelBase
    {
        /*
         * These are the documents being dragged. Use GetDropDocuments() to get the documents to drop.
         */

        public abstract List<DocumentController> GetDropDocuments(Point? where, Windows.UI.Xaml.FrameworkElement target, bool dontMove=false);

        public virtual bool CanDrop(FrameworkElement element)
        {
            return true;
        }

    }
}
