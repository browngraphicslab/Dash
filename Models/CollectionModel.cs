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

        public CollectionModel(ObservableCollection<DocumentView> docViews)
        {
            DocumentViews = docViews;
            DocumentViewModels = new ObservableCollection<DocumentViewModel>();
            Documents = new ObservableCollection<DocumentModel>();
            DocumentViewDict = new Dictionary<DocumentView, DocumentModel>();

            foreach (var docView in docViews)
            {
                DocumentModel doc = (docView.DataContext as DocumentViewModel)?.DocumentModel;
                DocumentViewModel model = docView.DataContext as DocumentViewModel;
                docView.ManipulationMode = ManipulationModes.System;
                docView.IsHitTestVisible = false;
                Documents.Add(doc);
                DocumentViewModels.Add(model);
                DocumentViewDict[docView] = doc;

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
