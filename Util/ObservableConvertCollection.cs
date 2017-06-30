using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    public class ObservableConvertCollection : ObservableCollection<FrameworkElement>
    {
        public ObservableCollection<DocumentModel> Documents;
        private Dictionary<DocumentModel, IList<FrameworkElement>> _dictionary = new Dictionary<DocumentModel, IList<FrameworkElement>>();
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
                        foreach (FrameworkElement elem in _dictionary[model])
                        {
                            base.Remove(elem);
                        }
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
            foreach (DocumentModel model in newDocs)
            {
                DocumentController controller = new DocumentController(model);
                var layout = controller.GetField(DashConstants.KeyStore.LayoutKey) as DocumentFieldModelController;
                var elements = layout != null
                    ? layout.Data.MakeViewUI()
                    : new DocumentViewModel(controller).GetUiElements(new Rect());

                if (elements.Count == 0)
                {
                    var panel = controller.MakeAllViewUI();
                    Add(panel);
                }
                else
                    _dictionary[model] = elements;
                foreach (var element in elements)
                {
                    base.Add(element);
                }
            }
        }
    }
}
