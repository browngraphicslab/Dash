using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class DocumentController
    {
        private Dictionary<int, DocumentModel> _documents;

        public static DocumentController Instance { get; } = new DocumentController();

        private DocumentController()
        {
            _documents = new Dictionary<int, DocumentModel>();
        }

        public DocumentModel GetDocumentWithId(int id)
        {
            return _documents[id];
        }

        public FieldModel GetFieldInDocument(int docId, string field)
        {
            DocumentModel model = _documents[docId];
            return model?.Fields[field];
        }

        public void AddDocument(DocumentModel model)
        {
            _documents[model.Id] = model;
        }
    }
}
