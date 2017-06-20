using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace DashServer
{
    /// <summary>
    ///     Serves as the connector for the cosmosDb database
    /// </summary>
    public class CosmosDb : IDocumentRepository
    {


        /// <summary>
        ///     The endpoint that we connect to in order to find the cosmosDb
        ///     endpoints found on azure portal under cosmosDb -> keys
        ///     endpointURL is the URI field,
        /// </summary>
        private const string EndpointUrl = DashConstants.DbEndpointUrl;

        /// <summary>
        ///     The key authorizing the cosmosDb to trust us
        ///     primarykey found on azure portal under cosmosDb -> keys
        ///     primarykey is the primary key field
        /// </summary>
        private readonly string PrimaryKey = DashConstants.DbPrimaryKey;

        /// <summary>
        ///     Clientside representation of cosmosDb service used to communicate with the database
        /// </summary>
        private DocumentClient _client;

        /// <summary>
        ///     Initialize a new connection to the database, should only be done once!
        /// </summary>
        public CosmosDb()
        {
            // if the _client isn't null then we are making unecessary connections to the database
            Debug.Assert(_client == null);

            try
            {
                _client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

                // Creates the database with the passed in id if it does not exist, and returns the database with the passed in id
                _client.CreateDatabaseIfNotExistsAsync(new Database
                {
                    Id = DashConstants.DocDbDatabaseId
                }).Wait();

                // Creates the collection with the passed in id if it does not exist, and returns the collection with the passed in id
                _client.CreateDocumentCollectionIfNotExistsAsync(GetDatabaseLink,
                    new DocumentCollection {Id = DashConstants.DocDbCollectionId }).Wait();
            }
            catch (DocumentClientException e)
            {
                Debug.WriteLine(e);
                throw;
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine("One or more exceptions has occurred:");
                foreach (var exception in ex.InnerExceptions)
                {
                    Debug.WriteLine("  " + exception.Message);
                }
                // If you throw here the database did not connect
                throw;
            }
        }

        /// <summary>
        ///     The link to the colletion in the database
        /// </summary>
        private Uri GetCollectionLink
            => UriFactory.CreateDocumentCollectionUri(DashConstants.DocDbDatabaseId, DashConstants.DocDbCollectionId);

        /// <summary>
        ///     The link to the database
        /// </summary>
        private Uri GetDatabaseLink => UriFactory.CreateDatabaseUri(DashConstants.DocDbDatabaseId);

        /// <summary>
        /// Returns a link to a document in the database
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        private Uri GetDocumentLink(string docId)
            => UriFactory.CreateDocumentUri(DashConstants.DocDbDatabaseId, DashConstants.DocDbCollectionId, docId);

        /// <summary>
        ///     Gets items from the cosmosDb
        /// </summary>
        /// <typeparam name="T">The class that we are going to deserialize items as</typeparam>
        /// <param name="predicate">The query we are using to get items from the database</param>
        /// <returns>An IEnumerable of the class that was desired</returns>
        public async Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var query = _client.CreateDocumentQuery<T>(GetCollectionLink)
                    .Where(predicate)
                    .AsDocumentQuery();

                var results = new List<T>();
                while (query.HasMoreResults)
                    results.AddRange(await query.ExecuteNextAsync<T>());

                return results;
            }
            catch (DocumentClientException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///     Add a list of items to the database
        /// </summary>
        /// <param name="items">The list of items to be added to the database</param>
        /// <typeparam name="T">The class type of each item in the list</typeparam>
        /// <returns>The added items</returns>
        public async Task<IEnumerable<T>> AddItemsAsync<T>(IEnumerable<T> items)
        {
            var results = new List<T>();

            try
            {
                // transfer over all the new models
                foreach (var item in items)
                {
                    var resourceResponse = await _client.CreateDocumentAsync(GetCollectionLink, item);
                    T result = (dynamic) resourceResponse.Resource;
                    results.Add(result);
                }

                return results;
            }
            catch (DocumentClientException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///     Add a single item to the database
        /// </summary>
        /// <typeparam name="T">The class type of the item that is going to be added</typeparam>
        /// <param name="item">The item which will be added to the database</param>
        /// <returns>The added item</returns>
        public async Task<T> AddItemAsync<T>(T item)
        {
            try
            {
                var resourceResponse = await _client.CreateDocumentAsync(GetCollectionLink, item);
                T result = (dynamic) resourceResponse.Resource;
                return result;
            }
            catch (DocumentClientException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Updates a number of items in the database
        /// </summary>
        /// <typeparam name="T">The class type of the items that are going to be updated</typeparam>
        /// <param name="items">The items that are going to be updated</param>
        /// <returns>The updated items</returns>
        public async Task<IEnumerable<T>> UpdateItemsAsync<T>(IEnumerable<T> items)
        {
            var results = new List<T>();

            try
            {
                // transfer over all the new models
                foreach (var item in items)
                {
                    var resourceResponse = await _client.ReplaceDocumentAsync(GetCollectionLink, item);
                    T result = (dynamic)resourceResponse.Resource;
                    results.Add(result);
                }

                return results;
            }
            catch (DocumentClientException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Updates a single item in the database
        /// </summary>
        /// <typeparam name="T">The class type of the item that is going to be updated</typeparam>
        /// <param name="item">The item that is going to be updated</param>
        /// <returns>The updated item</returns>
        public async Task<T> UpdateItemAsync<T>(T item)
        {
            try
            {
                var resourceResponse = await _client.ReplaceDocumentAsync(GetCollectionLink, item);
                T result = (dynamic)resourceResponse.Resource;
                return result;
            }
            catch (DocumentClientException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///     Deletes the item from the database with the passed in id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The document which was deleted from the database</returns>
        public async Task<Document> DeleteItemAsync(string id)
        {
            return await _client.DeleteDocumentAsync(GetDocumentLink(id));
        }


    }
}

