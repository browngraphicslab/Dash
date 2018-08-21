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
        Task AddDocument(T newDocument);

        /// <summary>
        ///     Updates a document on the server.
        /// </summary>
        /// <param name="documentToUpdate"></param>
        Task UpdateDocument(T documentToUpdate);

        /// <summary>
        ///     Gets a document from the server.
        /// </summary>
        /// <param name="id"></param>
        Task<T> GetDocument(string id);

        /// <summary>
        /// Gets documents from the server with the given ids
        /// </summary>
        /// <param name="ids"></param>
        Task<List<T>> GetDocuments(IEnumerable<string> ids);

        /// <summary>
        /// Gets documents from the server with the given ids but only returns the entities of type V
        /// </summary>
        /// <param name="ids"></param>
        Task<List<U>> GetDocuments<U>(IEnumerable<string> ids) where U : EntityBase;

        /// <summary>
        ///     Deletes a document from the server.
        /// </summary>
        /// <param name="document"></param>
        Task DeleteDocument(T document);

        /// <summary>
        ///     Deletes all input documents from the server.
        /// </summary>
        /// <param name="documents"></param>
        Task DeleteDocuments(IEnumerable<T> documents);

        /// <summary>
        ///     Deletes all documents from the server.
        /// </summary>
        Task DeleteAllDocuments();

        /// <summary>
        /// method to make an arbitrary query for a subset of document T's
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<List<EntityBase>> GetDocumentsByQuery(IQuery<T> query);

        /// <summary>
        /// method to make an arbitrary query for a subset of document T's but only returns the types V
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<List<U>> GetDocumentsByQuery<U>(IQuery<T> query) where U : EntityBase;

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
        Task<bool> HasDocument(T model, Action<bool> success, Action<Exception> error);

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
