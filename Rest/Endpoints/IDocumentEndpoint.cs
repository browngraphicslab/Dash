using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public interface IDocumentEndpoint
    {
        /// <summary>
        ///     Adds a document to the server.
        /// </summary>
        /// <param name="newDocument"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        Task AddDocument(DocumentModel newDocument, Action<DocumentModel> success, Action<Exception> error);

        /// <summary>
        ///     Updates a document on the server.
        /// </summary>
        /// <param name="documentToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void UpdateDocument(DocumentModel documentToUpdate, Action<DocumentModel> success,
            Action<Exception> error);

        /// <summary>
        ///     Gets a document from the server.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        Task GetDocument(string id, Func<DocumentModel, Task> success, Action<Exception> error);

        /// <summary>
        ///     Gets a document from the server.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        Task GetDocuments(IEnumerable<string> ids, Func<IEnumerable<DocumentModel>, Task> success, Action<Exception> error);

        /// <summary>
        ///     Deletes a document from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void DeleteDocument(DocumentModel document, Action success, Action<Exception> error);

        /// <summary>
        ///     Deletes all documents from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void DeleteAllDocuments(Action success, Action<Exception> error);

        Task GetDocumentByType(DocumentType documentType, Func<IEnumerable<DocumentModel>, Task> success, Action<Exception> error);
    }
}