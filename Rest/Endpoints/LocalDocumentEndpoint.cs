using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class LocalDocumentEndpoint : IDocumentEndpoint
    {
        public async Task AddDocument(DocumentModel newDocument, Action<DocumentModel> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void UpdateDocument(DocumentModel documentToUpdate, Action<DocumentModel> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task GetDocument(string id, Func<DocumentModelDTO, Task> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task GetDocuments(IEnumerable<string> ids, Func<IEnumerable<DocumentModelDTO>, Task> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void DeleteDocument(DocumentModel document, Action success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void DeleteAllDocuments(Action success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task GetDocumentByType(DocumentType documentType, Action<IEnumerable<DocumentModelDTO>> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }
    }
}
