using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Dash.Models
{
    public class CollectionModel
    {
        public ObservableCollection<DocumentModel> Documents;
        public ObservableCollection<DocumentViewModel> DocumentViewModels;
        public ObservableCollection<DocumentView> DocumentViews;
        public Dictionary<DocumentView, DocumentModel> DocumentViewDict;

        public CollectionModel(ObservableCollection<DocumentModel> documents)
        {
            DocumentViews = new ObservableCollection<DocumentView>();
            DocumentViewModels = new ObservableCollection<DocumentViewModel>();
            Documents = documents;
            DocumentViewDict = new Dictionary<DocumentView, DocumentModel>();

            foreach (var doc in documents)
            {
                DocumentViewModel model = new DocumentViewModel(doc, DocumentLayoutModelSource.DefaultLayoutModelSource);
                DocumentView view = new DocumentView {DataContext = model};
                view.ManipulationMode = ManipulationModes.System;
                view.IsHitTestVisible = false;
                DocumentViews.Add(view);
                DocumentViewModels.Add(model);
                DocumentViewDict[view] = doc;

            }
        }

        public void RemoveDocument(DocumentView view)
        {
            DocumentViewModels.Remove(view.DataContext as DocumentViewModel);
            Documents.Remove(DocumentViewDict[view]);
            DocumentViewDict[view] = null;
            DocumentViews.Remove(view);

        }

        //public DocumentView GetCopyOf(DocumentView view)
        //{
        //    DocumentViewModel model = new DocumentViewModel(DocumentViewDict[view], DocumentLayoutModelSource.DefaultLayoutModelSource);
        //    DocumentView newView = new DocumentView { DataContext = model };
        //    return newView;
        //}


        public void SetLayout()
        {
            
        }

    }
}
