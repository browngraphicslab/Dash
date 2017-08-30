using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentEndpoint
    {
        private readonly ServerEndpoint _connection;

        ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        private SemaphoreSlim _semaphore;

        public DocumentEndpoint(ServerEndpoint connection)
        {
            _connection = connection;

            // create a semaphore to be used in the get documents async task
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        ///     Adds a document to the server.
        /// </summary>
        /// <param name="newDocument"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async Task AddDocument(DocumentModel newDocument, Action<DocumentModel> success, Action<Exception> error)
        {
            try
            {
                if (!_semaphores.ContainsKey(newDocument.Id))
                {
                    _semaphores[newDocument.Id] = new SemaphoreSlim(1);
                }

                await _semaphores[newDocument.Id].WaitAsync();

                var result = await _connection.Post("api/Document", newDocument);
                var resultDoc = await result.Content.ReadAsAsync<DocumentModel>();

                success(resultDoc);
                _semaphores[newDocument.Id].Release();
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
                _semaphores[newDocument.Id].Release();
            }
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
            try
            {
                if (!_semaphores.ContainsKey(documentToUpdate.Id))
                {
                    _semaphores[documentToUpdate.Id] = new SemaphoreSlim(1);
                }

                await _semaphores[documentToUpdate.Id].WaitAsync();

                var result = await _connection.Put("api/Document", documentToUpdate);
                var resultDoc = await result.Content.ReadAsAsync<DocumentModel>();

                success(resultDoc);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
            finally
            {
                _semaphores[documentToUpdate.Id].Release();
            }
        }

        /// <summary>
        ///     Gets a document from the server.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async Task GetDocument(string id, Action<DocumentModelDTO> success, Action<Exception> error)
        {
            try
            {
                var result = await _connection.GetItem<DocumentModelDTO>($"api/Document/{id}");
                success(result);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }

        /// <summary>
        ///     Gets a document from the server.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async void GetDocuments(IEnumerable<string> ids, Action<IEnumerable<DocumentModelDTO>> success, Action<Exception> error)
        {

            try
            {
                await _semaphore.WaitAsync();

                var url = $"api/Document/batch/{string.Join("&", ids)}";
                var result = await _connection.GetItem<IEnumerable<DocumentModelDTO>>(url);
                success(result);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        ///     Deletes a document from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async void DeleteDocument(DocumentModel document, Action success, Action<Exception> error)
        {
            try
            {
                var response = await _connection.Delete($"api/Document/{document.Id}");
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
        }

        /// <summary>
        ///     Deletes all documents from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async void DeleteAllDocuments(Action success, Action<Exception> error)
        {
            try
            {
                var response = await _connection.Delete($"api/Document");
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
        }


        public async Task GetDocumentByType(DocumentType documentType, Action<IEnumerable<DocumentModelDTO>> success, Action<Exception> error)
        {
            try
            {
                var response = await _connection.GetItem<IEnumerable<DocumentModelDTO>>($"api/Document/type/{documentType.Id}");
                success(response);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }
    }
}