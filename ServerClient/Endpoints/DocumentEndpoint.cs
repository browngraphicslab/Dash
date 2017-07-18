using System.Net.Http;
using System.Threading.Tasks;
using DashShared;
using System.Diagnostics;

namespace Dash
{
    public class DocumentEndpoint
    {
        private ServerEndpoint _connection;

        public DocumentEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
        }


        /// <summary>
        /// Converts Dash client-side representation of the DocModel into the server-side DashShared DocumentModel
        /// </summary>
        private ServerDocumentModel convertToServerModel(DocumentModel newDocument) {
            return new ServerDocumentModel(newDocument.Fields, newDocument.DocumentType, newDocument.Id);
        }

        /// <summary>
        /// Converts Dash server-side representation of the DocModel into the client-side DashShared DocumentModel
        /// </summary>
        private DocumentModel convertToClientModel(ServerDocumentModel newDocument)
        {
            return new DocumentModel(newDocument.Fields, newDocument.DocumentType);
        }

        /// <summary>
        /// Adds a new Document to the DashWebServer and returns that DocumentModel.
        /// </summary>
        /// <param name="newDocument"></param>
        /// <returns></returns>
        public async Task<Result<DocumentModel>> AddDocument(DocumentModel newDocument)
        {
            try
            {
                // convert from Dash DocumentModel to DashShared DocumentModel (server representation)
                HttpResponseMessage result = _connection.Post("api/Document", convertToServerModel(newDocument));
                ServerDocumentModel resultdoc = await result.Content.ReadAsAsync<ServerDocumentModel>();
                return new Result<DocumentModel>(true,convertToClientModel(resultdoc));
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<DocumentModel>(false, string.Join("\n", e.Errors));
            }
        }

        /// <summary>
        /// Updates an existing Document in the DashWebServer and returns the updated document model.
        /// </summary>
        /// <param name="DocumentToUpdate"></param>
        /// <returns></returns>
        public async Task<Result<DocumentModel>> UpdateDocument(DocumentModel DocumentToUpdate)
        {
            try
            {
                HttpResponseMessage result = _connection.Put("api/Document",convertToServerModel(DocumentToUpdate));
                DocumentModel resultdoc = await result.Content.ReadAsAsync<DocumentModel>();
                return new Result<DocumentModel>(true, resultdoc);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<DocumentModel>(false, string.Join("\n", e.Errors));
            }
            
        }

        /// <summary>
        /// Fetches a document with the given ID from the server.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Result<DocumentModel>> GetDocument(string id)
        {
            try
            {
                ServerDocumentModel result = await _connection.GetItem<ServerDocumentModel>($"api/Field/{id}");
                return new Result<DocumentModel>(true,convertToClientModel(result));
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<DocumentModel>(false, string.Join("\n", e.Errors));
            }
        }

        /// <summary>
        /// Deletes a document with the given ID from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public Result DeleteDocument(DocumentModel document)
        {
            string id = document.Id;
            try
            {
                _connection.Delete($"api/Document/{id}");
                return new Result(true);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result(false, string.Join("\n", e.Errors));
            }
        }
    }
}
