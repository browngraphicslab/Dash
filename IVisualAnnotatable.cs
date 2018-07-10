using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Dash
{
    public interface IAnnotatable
    {
        void RegionSelected(object region, Point pt, DocumentController chosenDoc = null);
    }

    public interface IVisualAnnotatable : IAnnotatable
    {
        FrameworkElement Self();
        Size GetTotalDocumentSize();
        // the UIElement to figure out where the pointer is relative to
        FrameworkElement GetPositionReference();
        // since this is different for every class, they must implement this part themselves
        DocumentController GetDocControllerFromSelectedRegion();
        VisualAnnotationManager GetAnnotationManager();

        // Invoke these when the region preview boxes should be altered
        event PointerEventHandler NewRegionStarted;
        event PointerEventHandler NewRegionMoved;
        event PointerEventHandler NewRegionEnded;
    }
}
