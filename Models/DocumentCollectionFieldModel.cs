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
        private ObservableCollection<DocumentModel> _docs = new ObservableCollection<DocumentModel>();

        public DocumentCollectionFieldModel(IEnumerable<DocumentModel> docs)
        {
            foreach (var d in docs)
                _docs.Add(d);
        }

        public ObservableCollection<DocumentModel> Documents { get { return _docs; } }

        public void AddDocumentModel(DocumentModel doc)
        {
            _docs.Add(doc);
        }

        public IEnumerable<DocumentModel> EnumDocuments()
        {
            return _docs;
        }

    }
}