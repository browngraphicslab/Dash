using System;
using System.Collections.Generic;
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
        /// Gets documents from the server with the given ids but only returns the entities of type V
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        Task GetDocuments<V>(IEnumerable<string> ids, Func<IEnumerable<V>, Task> success,Action<Exception> error) where V : EntityBase;

        /// <summary>
        ///     Deletes a document from the server.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void DeleteDocument(T document, Action success, Action<Exception> error);

        /// <summary>
        ///     Deletes all input documents from the server.
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void DeleteDocuments(IEnumerable<T> documents, Action success, Action<Exception> error);

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

        /// <summary>
        /// method to make an arbitrary query for a subset of document T's but only returns the types V
        /// </summary>
        /// <param name="query"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        Task GetDocumentsByQuery<V>(IQuery<T> query, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase;

        /// <summary>
        /// Close the connection to the endpoint
        /// </summary>
        /// <returns></returns>
        Task Close();

        /// <summary>
        /// Whether or not the end point has the passed in document
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void HasDocument(T model, Action<bool> success, Action<Exception> error);

        bool CheckAllDocuments(IEnumerable<T> documents);

        /// <summary>
        /// Returns a list of backups in the format, pretty print string, connection string (i.e. path, connect to database etc...)
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetBackups();

        /// <summary>
        /// Set the interval between backups in milliseconds
        /// </summary>
        /// <param name="millis"></param>
        void SetBackupInterval(int millis);

        /// <summary>
        /// Set the max number of backups that should be maintained
        /// </summary>
        /// <param name="numBackups"></param>
        void SetNumBackups(int numBackups);
    }
}
