using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public interface ICollectionViewModel
    {
        bool IsInterfaceBuilder { get; set; }

        ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; }

        double CellSize { get; set; }
        bool CanDragItems { get; set; }
        ListViewSelectionMode ItemSelectionMode { get; set; }

        void AddDocuments(List<DocumentController> documents, Context context);
        void AddDocument(DocumentController document, Context context);
        void RemoveDocuments(List<DocumentController> documents);
        void RemoveDocument(DocumentController document);
    }
}