using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class DocumentCanvasViewModel : ViewModelBase
    {
        private readonly bool _isInInterfaceBuilder;
        private ObservableCollection<DocumentViewModel> _documentViews;

        public ObservableCollection<DocumentViewModel> DocumentViews
        {
            get { return _documentViews; }
            set { SetProperty(ref _documentViews, value); }
        }

        public DocumentCanvasViewModel(bool isInInterfaceBuilder)
        {
            _isInInterfaceBuilder = isInInterfaceBuilder;
            DocumentViews = new ObservableCollection<DocumentViewModel>();
        }


        public void AddDocument(DocumentController newDocument, bool isInInterfaceBuilder)
        {
            var docVm = new DocumentViewModel(newDocument, isInInterfaceBuilder);

            DocumentViews.Add(docVm);
        }

        public void RemoveDocument(DocumentController docToBeRemoved)
        {
            var docVmToRemove = DocumentViews.FirstOrDefault(docVm => docVm.DocumentController.GetId().Equals(docToBeRemoved.GetId()));
            if (docVmToRemove != null)
            {
                DocumentViews.Remove(docVmToRemove);
            }
        }
    }
}
