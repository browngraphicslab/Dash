﻿using System;
using System.Collections.Generic;
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
            try
            {
                var result = await _connection.Post("api/Document", newDocument);
                var resultDoc = await result.Content.ReadAsAsync<DocumentModel>();

                success(resultDoc);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
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
                var result = await _connection.Put("api/Document", documentToUpdate);
                var resultDoc = await result.Content.ReadAsAsync<DocumentModel>();

                success(resultDoc);
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
        public async void GetDocument(string id, Action<DocumentModelDTO> success, Action<Exception> error)
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
                var result = await _connection.GetItem<IEnumerable<DocumentModelDTO>>($"api/Document/batch/{string.Join(",", ids)}");
                success(result);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
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


        public async Task GetDocumentByType(DocumentType mainDocumentType, Action<IEnumerable<DocumentModelDTO>> success, Action<Exception> error)
        {
            try
            {
                var response = await _connection.GetItem<IEnumerable<DocumentModelDTO>>($"api/Document/type/{mainDocumentType.Id}");
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