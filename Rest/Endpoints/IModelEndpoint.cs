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
        Task AddDocument(Controller<T> newDocument);

        /// <summary>
        ///     Updates a document on the server.
        /// </summary>
        /// <param name="documentToUpdate"></param>
        Task UpdateDocument(Controller<T> documentToUpdate);

        /// <summary>
        ///     Deletes a document from the server.
        /// </summary>
        /// <param name="document"></param>
        Task DeleteDocument(Controller<T> document);

        /// <summary>
        ///     Deletes all input documents from the server.
        /// </summary>
        /// <param name="documents"></param>
        Task DeleteDocuments(IEnumerable<Controller<T>> documents);

        /// <summary>
        ///     Deletes all documents from the server.
        /// </summary>
        Task DeleteAllDocuments();

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

        FieldControllerBase GetController(string id);
        IList<FieldControllerBase> GetControllers(IEnumerable<string> ids);
        V GetController<V>(string id) where V : FieldControllerBase;
        IList<V> GetControllers<V>(IEnumerable<string> ids) where V : FieldControllerBase;

        Task<FieldControllerBase> GetControllerAsync(string id);
        Task<IList<FieldControllerBase>> GetControllersAsync(IEnumerable<string> ids);
        Task<V> GetControllerAsync<V>(string id) where V : FieldControllerBase;
        Task<IList<V>> GetControllersAsync<V>(IEnumerable<string> ids) where V : FieldControllerBase;
        Task<IList<FieldControllerBase>> GetControllersByQueryAsync(IQuery<T> query);
        Task<IList<V>> GetControllersByQueryAsync<V>(IQuery<T> query) where V : FieldControllerBase;


        /// <summary>
        /// method to make an arbitrary query for a subset of document T's
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<List<T>> GetDocumentsByQuery(IQuery<T> query);

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
        Task<bool> HasDocument(T model);

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
