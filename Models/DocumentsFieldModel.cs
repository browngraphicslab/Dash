using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Dash.Models;

namespace Dash
{
    public class DocumentsFieldModel : FieldModel
    {
        private List<DocumentModel> _docs;

        public DocumentsFieldModel(List<DocumentModel> docs)
        {
            _docs = docs;
        }

        public override UIElement MakeView(TemplateModel template)
        {
            var docViews = new List<DocumentView>();
            foreach (var docModel in _docs)
            {
                DocumentViewModel docVM = new DocumentViewModel(docModel, DocumentLayoutModelSource.DefaultLayoutModelSource);
                DocumentView docView = new DocumentView();
                docView.DataContext = docVM;
                docViews.Add(docView);
            }

            var observableDocs = new ObservableCollection<DocumentView>(_docs);
            var collectionModel = new CollectionModel();
        }
    }
}