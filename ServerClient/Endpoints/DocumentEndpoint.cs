using System.Net.Http;
using System.Threading.Tasks;
using DashShared;

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
                // convert from Document model to DTO
                HttpResponseMessage result = _connection.Post("api/Document", newDocument);
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
        /// Updates an existing Document in the DashWebServer and returns the updated document model.
        /// </summary>
        /// <param name="DocumentToUpdate"></param>
        /// <returns></returns>
        public async Task<Result<DocumentModel>> UpdateDocument(DocumentModel DocumentToUpdate)
        {
            try
            {
                HttpResponseMessage result = _connection.Put("api/Document", DocumentToUpdate);
                DocumentModel resultdoc = await result.Content.ReadAsAsync<DocumentModel>();
                return new Result<DocumentModel>(true, resultdoc);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<DocumentModel>(false, string.Join("\n", e.Errors));
            }
            
        }

        public async Task<Result<DocumentModel>> GetDocument(string id)
        {
            try
            {
                DocumentModel result = await _connection.GetItem<DocumentModel>($"api/Field/{id}");
                return new Result<DocumentModel>(true, result);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<DocumentModel>(false, string.Join("\n", e.Errors));
            }
        }

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
