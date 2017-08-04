using System.Net.Http;
using System.Threading.Tasks;
using DashShared;
using System.Diagnostics;
using System.Collections.Generic;

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
        /// Adds a new Document to the DashWebServer and returns that DocumentModel.
        /// </summary>
        /// <param name="newDocument"></param>
        /// <returns></returns>
        public async Task<Result<DocumentModel>> AddDocument(DocumentModel newDocument)
        {
            try
            {
                // convert from Dash DocumentModel to DashShared DocumentModel (server representation)
                HttpResponseMessage result = _connection.Post("api/Document", newDocument);
                var resultdoc = await result.Content.ReadAsAsync<DocumentModel>();
                return new Result<DocumentModel>(true, resultdoc);
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
        public async Task<Result<DocumentModel>> UpdateDocument(DocumentModel documentToUpdate)
        {
            try
            {
                HttpResponseMessage result = _connection.Put("api/Document", documentToUpdate);
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
                var result = await _connection.GetItem<DocumentModel>($"api/Document/{id}");
                return new Result<DocumentModel>(true, result);
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
