using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public class ManipulationControlHelper
    {
        private DocumentView _manipulationDocumentTarget = null;
        private int          _numMovements = 0;

        public ManipulationControlHelper(FrameworkElement eventElement, PointerRoutedEventArgs pointer, bool drillDown, bool useCache = false)
        {
            _manipulationDocumentTarget = eventElement.GetAncestorsOfType<DocumentView>().FirstOrDefault();
            if (useCache)
                eventElement.CacheMode = null;
        }
        
        public void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (++_numMovements == 2)
            {
                if (SelectionManager.TryInitiateDragDrop(_manipulationDocumentTarget, e, null))
                    e.Handled = true;
            }
        }
    }
}
