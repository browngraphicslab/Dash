using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class FreeFormCollectionViewModel : ViewModelBase, ICollectionViewModel
    {
        private ObservableCollection<DocumentViewModel> _documentViewModels;

        public FreeFormCollectionViewModel(bool isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            DocumentViewModels = new ObservableCollection<DocumentViewModel>();
            CellSize = 250;
            CanDragItems = true;
        }

        public bool IsInterfaceBuilder { get; set; }

        public ObservableCollection<DocumentViewModel> DocumentViewModels
        {
            get { return _documentViewModels; }
            set { SetProperty(ref _documentViewModels, value); }
        }

        public double CellSize { get; set; }
        public bool CanDragItems { get; set; }
        public ListViewSelectionMode ItemSelectionMode { get; set; }


        public void AddDocuments(List<DocumentController> documents, Context context)
        {
            foreach (var docController in documents)
                AddDocument(docController, context);
        }

        public void AddDocument(DocumentController document, Context context)
        {
            var docVm = new DocumentViewModel(document, IsInterfaceBuilder);
            DocumentViewModels.Add(docVm);
        }

        public void RemoveDocuments(List<DocumentController> documents)
        {
            foreach (var doc in documents)
                RemoveDocument(doc);
        }

        public void RemoveDocument(DocumentController document)
        {
            var vmToRemove = DocumentViewModels.FirstOrDefault(vm => vm.DocumentController.GetId() == document.GetId());
            if (vmToRemove != null)
                DocumentViewModels.Remove(vmToRemove);
        }
    }
}