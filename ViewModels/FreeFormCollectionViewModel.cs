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

        public FreeFormCollectionViewModel(bool isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            DocumentViewModels = new ObservableCollection<DocumentViewModel>();
        }


        public void AddDocuments(List<DocumentController> documents, Context context)
        {
            foreach (var docController in documents)
            {
                var docVm = new DocumentViewModel(docController, IsInterfaceBuilder);
                DocumentViewModels.Add(docVm);
            }
        }

        public void AddDocument(DocumentController document, Context context)
        {
            throw new NotImplementedException();
        }

        public void RemoveDocuments(List<DocumentController> documents)
        {
            var docsToRemove = new HashSet<string>(documents.Select(doc => doc.GetId()).ToList());

            foreach (var id in docsToRemove)
            {
                var vmToRemove = DocumentViewModels.FirstOrDefault(vm => vm.DocumentController.GetId() == id);
                if (vmToRemove != null)
                {
                    DocumentViewModels.Remove(vmToRemove);
                }

            }

        }

        public void RemoveDocument(DocumentController document)
        {
            throw new NotImplementedException();
        }
    }
}
