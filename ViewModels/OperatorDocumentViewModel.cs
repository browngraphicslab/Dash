using Dash.Models.OperatorModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash.ViewModels
{
    public class OperatorDocumentViewModel : DocumentViewModel
    {
        //public OperatorDocumentViewModel(OperatorDocumentModel document) : base(document)
        //{
        //}

        /// <summary>
        /// Overrides DocumentViewModel.GetUiElements to just return the operators view
        /// and listens to the operator view's IO event
        /// </summary>
        /// <returns></returns>
        //public override List<FrameworkElement> GetUiElements(Rect bounds)
        //{
        //    var uiElements = base.GetUiElements(bounds);
        //    foreach (var uiele in uiElements)
        //    {
        //        var opView = uiele as OperatorView;
        //        if (opView != null)
        //        {
        //            opView.IoDragStarted += View_IODragStarted;
        //            opView.IoDragEnded += View_IoDragEnded;
        //        }
        //    }
        //    return uiElements;
        //}

        //private void View_IoDragEnded(OperatorView.IOReference ioReference)
        //{
        //    IODragEnded?.Invoke(ioReference);
        //}

        //private void View_IODragStarted(OperatorView.IOReference ioReference)
        //{
        //    IODragStarted?.Invoke(ioReference);
        //}

        //public event OperatorView.IODragEventHandler IODragStarted;
        //public event OperatorView.IODragEventHandler IODragEnded;
    }
}
