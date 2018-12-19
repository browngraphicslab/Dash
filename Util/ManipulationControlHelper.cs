using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public class ManipulationControlHelper
    {
        public DocumentView _manipulationDocumentTarget = null;
        private int          _numMovements = 0;

        public ManipulationControlHelper(FrameworkElement eventElement, bool useCache = false)
        {
            _manipulationDocumentTarget = eventElement.GetAncestorsOfType<DocumentView>().FirstOrDefault();
            if (useCache)
                eventElement.CacheMode = null;
        }
        
        public void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (++_numMovements == 2)
            {
                SelectionManager.InitiateDragDrop(_manipulationDocumentTarget, e);
                e.Handled = true;
            }
        }
    }
}
