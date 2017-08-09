using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DashShared;

namespace DashWebServer
{
    /// <summary>
    ///     The base class for our document repository, defines an interface so that we can
    ///     implement our document respository through dependency injection
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        ///     Add a list of items to the database
        /// </summary>
        /// <param name="items">The list of items to be added to the database</param>
        /// <typeparam name="T">The class type of each document in the list</typeparam>
        /// <returns>The added items</returns>
        Task<IEnumerable<T>> AddItemsAsync<T>(IEnumerable<T> items) where T : EntityBase;

        /// <summary>
        ///     Add a single document to the database
        /// </summary>
        /// <typeparam name="T">The class type of the document that is going to be added</typeparam>
        /// <param name="document">The document which will be added to the database</param>
        /// <returns>The added document</returns>
        Task<T> AddItemAsync<T>(T document) where T : EntityBase;

        /// <summary>
        ///     Gets items from the cosmosDb
        /// </summary>
        /// <typeparam name="T">The class that we are going to deserialize items as</typeparam>
        /// <param name="predicate">The query we are using to get items from the database</param>
        /// <returns>An IEnumerable of the class that was desired</returns>
        Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate) where T : EntityBase;

        /// <summary>
        ///     Gets an item from the database using its documentId. This method is faster than querying using
        ///     <see cref="CosmosDb.GetItemsAsync{T}" /> since the
        ///     item can be retrieved from an in memory cache, or accessed through a direct link in the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="documentId"></param>
        /// <returns></returns>
        Task<T> GetItemByIdAsync<T>(string documentId) where T : EntityBase;

        /// <summary>
        ///     Gets an item from the database using its documentId. This method is faster than querying using
        ///     <see cref="CosmosDb.GetItemsAsync{T}" /> since the
        ///     item can be retrieved from an in memory cache, or accessed through a direct link in the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="documentIds"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> GetItemsByIdAsync<T>(IEnumerable<string> documentIds) where T : EntityBase;

        /// <summary>
        ///     Updates a number of items in the database
        /// </summary>
        /// <typeparam name="T">The class type of the items that are going to be updated</typeparam>
        /// <param name="items">The items that are going to be updated</param>
        /// <returns>The updated items</returns>
        Task<IEnumerable<T>> UpdateItemsAsync<T>(IEnumerable<T> items) where T : EntityBase;

        /// <summary>
        ///     Updates a single document in the database
        /// </summary>
        /// <typeparam name="T">The class type of the document that is going to be updated</typeparam>
        /// <param name="document">The document that is going to be updated</param>
        /// <returns>The updated document</returns>
        Task<T> UpdateItemAsync<T>(T document) where T : EntityBase;

        /// <summary>
        ///     Deletes the document from the database with the passed in documentId
        /// </summary>
        /// <param name="document">The document that is going to be deleted</param>
        /// <returns>The document which was deleted from the database</returns>
        Task DeleteItemAsync<T>(T document) where T : EntityBase;

        int FieldCount { get; set; }

        object Lock { get; set; }
    }
}