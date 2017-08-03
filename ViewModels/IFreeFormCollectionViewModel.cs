using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dash
{
    public interface IFreeFormCollectionViewModel
    {
        bool IsInterfaceBuilder { get; set; }

        ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; }

        void AddDocuments(List<DocumentController> documents, Context context);
        void AddDocument(DocumentController document, Context context);
        void RemoveDocuments(List<DocumentController> documents);
        void RemoveDocument(DocumentController document);
    }
}