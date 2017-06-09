using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DashIntermediate;
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
        private readonly string EndpointUrl = DashConstants.DEVELOP_LOCALLY
            ? DashConstants.LocalEndpointUrl
            : DashConstants.ServerEndpointUrl;

        /// <summary>
        ///     The key authorizing the cosmosDb to trust us
        ///     primarykey found on azure portal under cosmosDb -> keys
        ///     primarykey is the primary key field
        /// </summary>
        private readonly string PrimaryKey = DashConstants.DEVELOP_LOCALLY
            ? DashConstants.LocalPrimaryKey
            : // this local key is always the same
            DashConstants.ServerPrimaryKey; // this secret key can be refreshed on the azure portal and might change

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
        /// <returns>Void</returns>
        public async Task AddItemsAsync<T>(IEnumerable<T> items)
        {
            try
            {
                // transfer over all the new models
                foreach (var item in items)
                    await _client.CreateDocumentAsync(GetCollectionLink, item);
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
        /// <returns>Void</returns>
        public async Task<Document> AddItemAsync<T>(T item)
        {
            try
            {
                return await _client.CreateDocumentAsync(GetCollectionLink, item);
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
        /// <returns></returns>
        public async Task<Document> DeleteItemAsync(string id)
        {
            return await _client.DeleteDocumentAsync(GetDocumentLink(id));
        }
    }
}

