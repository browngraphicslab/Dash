using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Dash.Models;

namespace Dash
{
    public class DocumentEndpoint
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
        private readonly TypeEndpoint _typeEndpoint;

        public DocumentEndpoint(TypeEndpoint typeEndpoint)
        {
            _typeEndpoint = typeEndpoint;
            _documents = new Dictionary<string, DocumentModel>();
        }

        public FieldModel GetFieldInDocument(string docId, Key field)
        {
            throw new NotImplementedException();

            //DocumentModel model = _documents[docId];
            //return model?.Field(field);
        }

        public FieldModel GetFieldInDocument(ReferenceFieldModel referenceFieldModel)
        {
            throw new NotImplementedException();

            //DocumentModel model = _documents[referenceFieldModel.DocId];
            //if (model != null)
            //{
            //    return model.Field(referenceFieldModel.FieldKey);
            //}
            //return null;
        }

        public IEnumerable<DocumentModel> GetDelegates(string protoId)
        {
            throw new NotImplementedException();

            //foreach (var doc in _documents)
            //{
            //    var docsProto = doc.Value.Field(DocumentModel.PrototypeKey) as DocumentModelFieldModel;
            //    if (docsProto != null && docsProto.Data.Id == protoId)
            //        yield return doc.Value;
            //}
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
            throw new NotImplementedException();

            //var id = $"{_numDocs++}";

            //var newDoc = new DocumentModel
            //{
            //    DocumentType = _typeEndpoint.CreateTypeAsync(type),
            //    Id = id
            //};

            //_documents[id] = newDoc;

            //return newDoc;
        }

        public DocumentModel CreateDocumentAsync(DocumentType type)
        {
            throw new NotImplementedException();

            //var id = $"{_numDocs++}";

            //var newDoc = new DocumentModel
            //{
            //    DocumentType = type,
            //    Id = id
            //};

            //_documents[id] = newDoc;

            //return newDoc;
        }
    }
}
