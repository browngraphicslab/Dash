using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class FreeFormCollectionViewModel : ViewModelBase, IFreeFormCollectionViewModel
    {
        private ObservableCollection<DocumentViewModel> _documentViewModels;

        public bool IsInterfaceBuilder { get; set; }
        public ObservableCollection<DocumentViewModel> DocumentViewModels
        {
            get { return _documentViewModels; }
            set { SetProperty(ref _documentViewModels, value); }
        }

        public double CellSize { get; set; }
        public bool CanDragItems { get; set; }

        public FreeFormCollectionViewModel(bool isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            DocumentViewModels = new ObservableCollection<DocumentViewModel>();
            CellSize = 250;
            CanDragItems = true;
        }


        public void AddDocuments(List<DocumentController> documents, Context context)
        {
            foreach (var docController in documents)
            {
                AddDocument(docController, context);
            }
        }

        public void AddDocument(DocumentController document, Context context)
        {
            var docVm = new DocumentViewModel(document, IsInterfaceBuilder);
            DocumentViewModels.Add(docVm);
        }

        public void RemoveDocuments(List<DocumentController> documents)
        {
            foreach (var doc in documents)
            {
                RemoveDocument(doc);
            }
        }

        public void RemoveDocument(DocumentController document)
        {
            var vmToRemove = DocumentViewModels.FirstOrDefault(vm => vm.DocumentController.GetId() == document.GetId());
            if (vmToRemove != null)
            {
                DocumentViewModels.Remove(vmToRemove);
            }
        }
    }
}
