using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Models;

namespace Dash
{
    public class DocumentCollectionFieldModel : FieldModel
    {
        private List<DocumentModel> _docs;

        public DocumentCollectionFieldModel(List<DocumentModel> docs)
        {
            _docs = docs;
        }

        public void AddDocumentModel(DocumentModel doc)
        {
            _docs.Add(doc);
        }

        public IEnumerable<DocumentModel> EnumDocuments()
        {
            return _docs;
        }

        public override FrameworkElement MakeView(TemplateModel template)
        {
            var collectionTemplate = template as DocumentCollectionTemplateModel;
            Debug.Assert(collectionTemplate != null);
            var collectionModel = new CollectionModel(new ObservableCollection<DocumentModel>(_docs));
            var collectionViewModel = new CollectionViewModel(collectionModel);
            var view = new CollectionView(collectionViewModel);

            Canvas.SetTop(view, collectionTemplate.Top);
            Canvas.SetLeft(view, collectionTemplate.Left);

            return view;
        }
    }
}