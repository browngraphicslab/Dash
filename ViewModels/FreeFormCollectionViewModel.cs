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
        private ObservableCollection<DocumentViewModel> _dataBindingSource;

        public bool IsInterfaceBuilder { get; set; }
        public ObservableCollection<DocumentViewModel> DataBindingSource
        {
            get { return _dataBindingSource; }
            set { SetProperty(ref _dataBindingSource, value); }
        }

        public FreeFormCollectionViewModel(bool isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            DataBindingSource = new ObservableCollection<DocumentViewModel>();
        }


        public void AddViewModels(List<DocumentController> documents, Context context)
        {
            foreach (var docController in documents)
            {
                var docVm = new DocumentViewModel(docController, IsInterfaceBuilder);
                DataBindingSource.Add(docVm);
            }
        }

        public void RemoveViewModels(List<DocumentController> documents)
        {
            var docsToRemove = new HashSet<string>(documents.Select(doc => doc.GetId()).ToList());

            foreach (var id in docsToRemove)
            {
                var vmToRemove = DataBindingSource.FirstOrDefault(vm => vm.DocumentController.GetId() == id);
                if (vmToRemove != null)
                {
                    DataBindingSource.Remove(vmToRemove);
                }

            }

        }
    }
}
