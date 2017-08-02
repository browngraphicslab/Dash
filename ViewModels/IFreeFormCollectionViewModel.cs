using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dash
{
    public interface IFreeFormCollectionViewModel
    {
        bool IsInterfaceBuilder { get; set; }

        ObservableCollection<DocumentViewModel> DataBindingSource { get; set; }

        void AddViewModels(List<DocumentController> documents, Context context);
        void RemoveViewModels(List<DocumentController> documents);
    }
}