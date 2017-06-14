using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentController
    {

        #region RemovedFakeLocal

        /// <summary>
        /// Fake dictionary of string (document id) to document model
        /// </summary>
        private Dictionary<string, DocumentModel> _documents;

        /// <summary>
        /// The number of documents we currently have
        /// </summary>
        private int _numDocs;

        #endregion

        /// <summary>
        /// Controller for getting new types
        /// </summary>
        private readonly TypeController _typeController;

        public DocumentController(TypeController typeController)
        {
            _typeController = typeController;
            _documents = new Dictionary<string, DocumentModel>();
        }

        public FieldModel GetFieldInDocument(string docId, Key field)
        {
            DocumentModel model = _documents[docId];
            return model?.Fields[field];
        }

        public FieldModel GetFieldInDocument(ReferenceFieldModel referenceFieldModel)
        {
            DocumentModel model = _documents[referenceFieldModel.DocId];
            if (model != null && model.Fields.ContainsKey(referenceFieldModel.FieldKey))
            {
                return model.Fields[referenceFieldModel.FieldKey];
            }
            return null;
        }

        public DocumentModel GetDocumentAsync(string docId)
        {
            return _documents[docId];
        }

        public void DeleteDocumentAsync(DocumentModel model)
        {
            _documents.Remove(model.Id);
        }

        public DocumentModel UpdateDocumentAsync(DocumentModel model)
        {
            _documents[model.Id] = model;
            return model;
        }

        //TODO Remove this
        public string GetDocumentId()
        {
            return $"{_numDocs++}";
        }

        public DocumentModel CreateDocumentAsync(string type)
        {
            var id = $"{_numDocs++}";

            var newDoc = new DocumentModel
            {
                DocumentType = _typeController.CreateTypeAsync(type),
                Fields = new Dictionary<Key, FieldModel>(),
                Id = id
            };

            _documents[id] = newDoc;

            return newDoc;
        }

        public DocumentModel CreateDocumentAsync(DocumentType type)
        {
            var id = $"{_numDocs++}";

            var newDoc = new DocumentModel
            {
                DocumentType = type,
                Fields = new Dictionary<Key, FieldModel>(),
                Id = id
            };

            _documents[id] = newDoc;

            return newDoc;
        }
    }
}
