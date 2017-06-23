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
            SetDocuments(docs);
        }

        public ObservableCollection<DocumentModel> Documents { get { return _docs; } }

        public void AddDocumentModel(DocumentModel doc)
        {
            _docs.Add(doc);
        }

        public void SetDocuments(IEnumerable<DocumentModel> docs)
        {
            _docs.Clear();
            foreach (var d in docs)
            {
                _docs.Add(d);
            }
        }

        public IEnumerable<DocumentModel> EnumDocuments()
        {
            return _docs;
        }

    }
}