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
            _docs = new List<DocumentModel>(docs);
        }

        public List<DocumentModel> Documents { get { return _docs; } }

        public void AddDocumentModel(DocumentModel doc)
        {
            _docs.Add(doc);
        }

        public void SetDocuments(List<DocumentModel> docs)
        {
            _docs.Clear();
            _docs.AddRange(docs);
        }

        public IEnumerable<DocumentModel> EnumDocuments()
        {
            return _docs;
        }

    }
}