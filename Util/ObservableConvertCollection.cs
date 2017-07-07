using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    public class ObservableConvertCollection : ObservableCollection<FrameworkElement>
    {
        public ObservableCollection<DocumentModel> Documents;
        private Dictionary<DocumentModel, FrameworkElement> _dictionary = new Dictionary<DocumentModel, FrameworkElement>();
        private DocumentView _view;


        public ObservableConvertCollection(ObservableCollection<DocumentModel> documents, DocumentView view)
        {
            documents.CollectionChanged += DocumentsOnCollectionChanged;
            Documents = documents;
            _view = view;
            AddElements(documents);
        }

        private void DocumentsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine("hi");
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    foreach (DocumentModel model in e.OldItems)
                    {
                        base.Remove(_dictionary[model]);
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    AddElements(e.NewItems as IList<DocumentModel>);
                    break;
                default:
                    break;
            }
        }

        private void AddElements(IList<DocumentModel> newDocs)
        {
            foreach (var model in newDocs)
            {
                var controller = new DocumentController(model);
                var elements = controller.MakeViewUI();
                
                var view = controller.MakeViewUI();
                _dictionary[model] = view;
                Add(view);
            }
        }
    }
}
