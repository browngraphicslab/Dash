using Dash.Models.OperatorModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Dash.ViewModels
{
    public class OperatorDocumentViewModel : DocumentViewModel
    {
        public OperatorDocumentViewModel(OperatorDocumentModel document) : base(document)
        {
        }

        public override List<UIElement> CreateUIElements()
        {
            var uiElements = base.GetUiElements();
            foreach (var uiele in uiElements)
            {
                if (uiele is OperatorView)
                    (uiele as OperatorView).IoDragStarted += View_IODragStarted;
            }
            return uiElements;
        }

        private void View_IODragStarted(OperatorView.IOReference ioReference)
        {
            IODragStarted?.Invoke(ioReference);
        }

        public event OperatorView.IODragEventHandler IODragStarted;
    }
}
