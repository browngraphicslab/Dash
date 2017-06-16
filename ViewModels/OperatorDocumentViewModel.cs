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
        public OperatorDocumentViewModel(OperatorDocumentModel document, DocumentLayoutModelSource layoutModelSource) : base(document, layoutModelSource)
        {
        }

        public override List<UIElement> GetUiElements()
        {
            List<UIElement> elements = new List<UIElement>();
            LayoutModel model = DocumentViewModelSource.DocumentLayoutModel(DocumentModel);
            OperatorView view = DocumentModel.Fields[OperatorDocumentModel.OperatorKey].MakeView(model.Fields[OperatorDocumentModel.OperatorKey]) as OperatorView;
            Debug.Assert(view != null);
            view.IoDragStarted += View_IODragStarted;
            elements.Add(view);
            return elements;
        }

        private void View_IODragStarted(OperatorView.IOReference ioReference)
        {
            IODragStarted?.Invoke(ioReference);
        }

        public event OperatorView.IODragEventHandler IODragStarted;
    }
}
