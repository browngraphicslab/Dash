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
using Microsoft.Extensions.Caching.Memory;

// cache design which could be really nice http://pdalinis.blogspot.com/2013/06/auto-refresh-caching-for-net-using.html
// it could also make things a little more complicated (dependency injection with two instances of the same interface)
// current cache design is based off of https://codeopinion.com/documentdb-caching-tips/
// as well as https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory

namespace DashWebServer
{

    /// <summary>
    ///     Serves as the connector for the cosmosDb database
    ///     Based off of https://auth0.com/blog/documentdb-with-aspnetcore/
    /// </summary>
    public class CosmosDb : IDocumentRepository
    {
        #region PrivateVariables

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
        private readonly DocumentClient _client;

        /// <summary>
        ///     The memory cache is use to store documents in local memory so the database doesn't have to be queried on every
        ///     update
        ///     <para>
        ///         Further info can be found here https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory and
        ///         here https://codeopinion.com/documentdb-caching-tips/
        ///     </para>
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        #endregion
               
        /// <summary>
        ///     Initialize a new connection to the database, should only be done once!
        /// </summary>
        public CosmosDb(IMemoryCache memoryCache)
        {
            // get the memory cache
            _memoryCache = memoryCache;

            // if the _client isn't null then we are making unecessary connections to the database
            Debug.Assert(_client == null);

            try
            {
                _client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

                // Creates the database with the passed in documentId if it does not exist, and returns the database with the passed in documentId
                _client.CreateDatabaseIfNotExistsAsync(new Database
                {
                    Id = DashConstants.DocDbDatabaseId
                }).Wait();

                // Creates the collection with the passed in documentId if it does not exist, and returns the collection with the passed in documentId
                _client.CreateDocumentCollectionIfNotExistsAsync(GetDatabaseLink,
                    new DocumentCollection { Id = DashConstants.DocDbCollectionId }).Wait();
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
                    Debug.WriteLine("  " + exception.Message);
                // If you throw here the database did not connect
                throw;
            }
        }

        #region DatabaseLinkGenerators

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
        ///     Returns a link to a document in the database
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        private Uri GetDocumentLink(string docId)
        {
            return UriFactory.CreateDocumentUri(DashConstants.DocDbDatabaseId, DashConstants.DocDbCollectionId, docId);
        }

        #endregion

        #region CREATE

        /// <summary>
        ///     Add a list of items to the database
        /// </summary>
        /// <param name="items">The list of items to be added to the database</param>
        /// <typeparam name="T">The class type of each document in the list</typeparam>
        /// <returns>The added items</returns>
        public async Task<IEnumerable<T>> AddItemsAsync<T>(IEnumerable<T> items) where T : EntityBase
        {
            var results = new List<T>();

            try
            {
                // transfer over all the new models
                foreach (var item in items)
                {
                    results.Add(await AddItemAsync(item));
                }

                return results;
            }
            catch (DocumentClientException e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        ///     Add a single document to the database
        /// </summary>
        /// <typeparam name="T">The class type of the document that is going to be added</typeparam>
        /// <param name="document">The document which will be added to the database</param>
        /// <returns>The added document</returns>
        public async Task<T> AddItemAsync<T>(T item) where T : EntityBase
        {
            try
            {
                // we use upsert to replace the document if it exists or create a new one if it doesn't
                var resourceResponse = await _client.UpsertDocumentAsync(GetCollectionLink, item);

                T result = (dynamic)resourceResponse.Resource;
                // add the new document to the cache
                var result2 = AddDocumentToCache(result);
                return result;
            }
            catch (DocumentClientException e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        //public object Lock2 = new object();

        #endregion

        #region READ

        /// <summary>
        ///     Gets items from the cosmosDb
        /// </summary>
        /// <typeparam name="T">The class that we are going to deserialize items as</typeparam>
        /// <param name="predicate">The query we are using to get items from the database</param>
        /// <returns>An IEnumerable of the class that was desired</returns>
        public async Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate) where T : EntityBase
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
                Debug.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Gets an item from the database using its documentId. This method is faster than querying using <see cref="GetItemsAsync{T}"/> since the
        /// item can be retrieved from an in memory cache, or accessed through a direct link in the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="documentId"></param>
        /// <returns></returns>
        public async Task<T> GetItemByIdAsync<T>(string documentId) where T : EntityBase
        {
            var result = GetDocumentFromCacheOrNull<T>(documentId);
            if (result is null)
            {
                try
                {
                    var resourceResponse = await _client.ReadDocumentAsync(GetDocumentLink(documentId));
                    result = (dynamic)resourceResponse.Resource;
                }
                catch (DocumentClientException e)
                {
                    Debug.WriteLine(e);
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets an item from the database using its documentId. This method is faster than querying using <see cref="GetItemsAsync{T}"/> since the
        /// item can be retrieved from an in memory cache, or accessed through a direct link in the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="documentId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetItemsByIdAsync<T>(IEnumerable<string> documentIds) where T : EntityBase
        {
            var results = new List<T>();
            foreach (var documentId in documentIds)
            {
                results.Add(await GetItemByIdAsync<T>(documentId));
            }

            return results;
        }

        #endregion

        #region UPDATE

        /// <summary>
        ///     Updates a number of items in the database
        /// </summary>
        /// <typeparam name="T">The class type of the items that are going to be updated</typeparam>
        /// <param name="items">The items that are going to be updated</param>
        /// <returns>The updated items</returns>
        public async Task<IEnumerable<T>> UpdateItemsAsync<T>(IEnumerable<T> items) where T : EntityBase
        {
            var results = new List<T>();

            // transfer over all the new models
            foreach (var item in items)
            {
                results.Add(await UpdateItemAsync(item));
            }

            return results;
        }

        /// <summary>
        ///     Updates a single document in the database
        /// </summary>
        /// <typeparam name="T">The class type of the document that is going to be updated</typeparam>
        /// <param name="document">The document that is going to be updated</param>
        /// <returns>The updated document</returns>
        public async Task<T> UpdateItemAsync<T>(T document) where T : EntityBase
        {
            try
            {
                // add the documetn to the cache and return it, it will update itself in the database when it needs to
                var result = AddDocumentToCache(document);
                return result;
            }
            catch (DocumentClientException e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Updates a single document in the database but skips the cache, this should only be called by the cache callback
        /// </summary>
        /// <typeparam name="T">The class type of the document that is going to be updated</typeparam>
        /// <param name="document">The document that is going to be updated</param>
        /// <returns>Nothing</returns>
        private async Task UpdateItemAsyncSkipCache<T>(T document) where T : EntityBase
        {
            try
            {
                // we use upsert to replace the document if it exists or create a new one if it doesn't
                await _client.UpsertDocumentAsync(GetCollectionLink, document);
            }
            catch (DocumentClientException e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }
        #endregion

        #region DELETE

        /// <summary>
        ///     Deletes the document from the database with the passed in documentId
        /// </summary>
        /// <param name="document">The document that is going to be deleted</param>
        /// <returns>The document which was deleted from the database</returns>
        public async Task DeleteItemAsync<T>(T document) where T : EntityBase
        {
            try
            {
                RemoveDocumentFromCache(document);
                var resourceResponse = await _client.DeleteDocumentAsync(GetDocumentLink(document.Id));
            }
            catch (DocumentClientException e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        #endregion

        #region MemoryCacheFunctionality

        /// <summary>
        /// Get a document from the cache if it is in the cache, otherwise returns null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="document"></param>
        /// <returns></returns>
        private T GetDocumentFromCacheOrNull<T>(T document) where T : EntityBase
        {
            return GetDocumentFromCacheOrNull<T>(document.Id);
        }

        /// <summary>
        /// Get a document from the cache if it is in the cache, otherwise returns null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="document"></param>
        /// <returns></returns>
        private T GetDocumentFromCacheOrNull<T>(string document) where T : EntityBase
        {

            if (_memoryCache.TryGetValue(GetMemoryCacheKeyFromDocument(document), out T cacheEntry))
            {
                //var ac = new AccessCondition { Condition = cacheEntry.ETag, Type = AccessConditionType.IfNoneMatch };
                //var response = await client.ReadDocumentAsync(cacheEntry.SelfLink, new RequestOptions { AccessCondition = ac });
                //if (response.StatusCode == HttpStatusCode.NotModified)
                //{
                return cacheEntry;
                //}
                //cacheEntry = response.Resource;
            }
            //else
            //{
            //    cacheEntry = (from f in client.CreateDocumentQuery(collectionLink, new FeedOptions { EnableCrossPartitionQuery = true })
            //                  where f.Id == documentId
            //                  select f).AsEnumerable().FirstOrDefault();
            //}

            //_memoryCache.Set(cacheKey, cacheEntry);
            //return cacheEntry;
            return null;
        }

        /// <summary>
        /// Adds the passed in document to the cache
        /// </summary>
        private T AddDocumentToCache<T>(T document) where T : EntityBase
        {
            return _memoryCache.Set(GetMemoryCacheKeyFromDocument(document), document, GetMemoryCacheEntryPolicy());
        }

        /// <summary>
        /// Removes the passed in document from the cache
        /// </summary>
        private void RemoveDocumentFromCache<T>(T document) where T : EntityBase
        {
            _memoryCache.Remove(GetMemoryCacheKeyFromDocument(document));
        }

        /// <summary>
        /// Adds cache entries to the database when they are evicted, but ignore the callback if they are removed
        /// manually (deleted on purpose)
        /// </summary>
        private async void OnEntryEvictedFromCache(object key, object value, EvictionReason reason, object state)
        {

            if (reason == EvictionReason.Removed)
            {
                return;
            }

            Debug.Assert(value is EntityBase);
            await UpdateItemAsyncSkipCache(value as EntityBase);
        }

        #endregion

        #region MemoryCacheHelpers

        /// <summary>
        /// helper method to provide a consistent way of generating unique cache keys for documents
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        private string GetMemoryCacheKeyFromDocument(string documentId)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(documentId));
            return $"{GetCollectionLink}:{documentId}";
        }

        /// <summary>
        /// Overload method to make getting cache keys from anything that inherits from entity base a little nicer
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private string GetMemoryCacheKeyFromDocument(EntityBase document)
        {
            return GetMemoryCacheKeyFromDocument(document.Id);
        }

        /// <summary>
        /// Method to generate the policy we use to store entries in the cache, this includes the registration of
        /// callbacks to add the data to the database when the data is being evicted.
        /// </summary>
        /// <returns></returns>
        private MemoryCacheEntryOptions GetMemoryCacheEntryPolicy()
        {
            return new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(2), // remove any data from the cache that hasn't been accessed for this amount of time
                PostEvictionCallbacks =
                {
                    new PostEvictionCallbackRegistration
                    {
                        EvictionCallback = OnEntryEvictedFromCache // whenever data is removed this method is called
                    }
                }
            };
        }
        

        #endregion
    }
}