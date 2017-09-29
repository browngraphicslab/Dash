using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public interface IModelEndpoint<T> where T:EntityBase
    {
        /// <summary>
        ///     Adds a document to the server.
        /// </summary>
        /// <param name="newDocument"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void AddDocument(T newDocument, Action<T> success, Action<Exception> error);

        /// <summary>
        ///     Updates a document on the server.
        /// </summary>
        /// <param name="documentToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void UpdateDocument(T documentToUpdate, Action<T> success,  Action<Exception> error);

        /// <summary>
        ///     Gets a document from the server.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error);

        /// <summary>
        /// Gets documents from the server with the given ids
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error);

        /// <summary>
        ///     Deletes a document from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void DeleteDocument(T document, Action success, Action<Exception> error);

        /// <summary>
        ///     Deletes all documents from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void DeleteAllDocuments(Action success, Action<Exception> error);

        /// <summary>
        /// method to make an arbitrary query for a subset of document T's
        /// </summary>
        /// <param name="query"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        Task GetDocumentsByQuery(IQuery<T> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error);
    }
}
