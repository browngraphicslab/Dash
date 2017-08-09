using System;
using System.Net.Http;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentEndpoint
    {
        private readonly ServerEndpoint _connection;

        public DocumentEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
        }

        /// <summary>
        ///     Adds a document to the server.
        /// </summary>
        /// <param name="newDocument"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async void AddDocument(DocumentModel newDocument, Action<DocumentModel> success, Action<Exception> error)
        {
            await _connection.TaskQueue.Enqueue(() => Task.Run(() =>
            {
                try
                {
                    var result = _connection.Post("api/Document", newDocument);
                    var resultDoc = result.Content.ReadAsAsync<DocumentModel>().Result;

                    success(resultDoc);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            }));
        }

        /// <summary>
        ///     Updates a document on the server.
        /// </summary>
        /// <param name="documentToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async void UpdateDocument(DocumentModel documentToUpdate, Action<DocumentModel> success,
            Action<Exception> error)
        {
            await _connection.TaskQueue.Enqueue(() => Task.Run(() =>
            {
                try
                {
                    var result = _connection.Put("api/Document", documentToUpdate);
                    var resultDoc = result.Content.ReadAsAsync<DocumentModel>().Result;

                    success(resultDoc);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            }));
        }

        /// <summary>
        ///     Gets a document from the server.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void GetDocument(string id, Action<DocumentModel> success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                try
                {
                    var result = _connection.GetItem<DocumentModel>($"api/Document/{id}").Result;
                    success(result);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }

        /// <summary>
        ///     Deletes a document from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void DeleteDocument(DocumentModel document, Action success, Action<Exception> error)
        {
            var id = document.Id;
            Task.Run(() =>
            {
                try
                {
                    var response = _connection.Delete($"api/Document/{id}");
                    if (response.IsSuccessStatusCode)
                        success();
                    else
                        error(new ApiException(response));
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }
    }
}