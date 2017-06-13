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

        public override UIElement MakeView(TemplateModel template)
        {
            var collectionTemplate = template as DocumentCollectionTemplateModel;
            Debug.Assert(collectionTemplate != null);

            var docViews = new ObservableCollection<DocumentView>();
            foreach (var docModel in _docs)
            {
                DocumentViewModel docVM = new DocumentViewModel(docModel, DocumentLayoutModelSource.DefaultLayoutModelSource);
                DocumentView docView = new DocumentView();
                docView.DataContext = docVM;
                docViews.Add(docView);
            }

            var collectionModel = new CollectionModel(docViews);
            var collectionViewModel = new CollectionViewModel(collectionModel);
            var view = collectionViewModel.View;

            Canvas.SetTop(view, collectionTemplate.Top);
            Canvas.SetLeft(view, collectionTemplate.Left);

            return view;
        }
    }
}