using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Azure.Documents;

namespace DashWebServer
{
    /// <summary>
    /// The base class for our document repository, defines an interface so that we can
    /// implement our document respository through dependency injection
    /// </summary>
    public interface IDocumentRepository
    {
        // See comments in Database/CosmosDb.cs
        Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate) where T : EntityBase;

        Task<IEnumerable<T>> AddItemsAsync<T>(IEnumerable<T> items) where T : EntityBase;

        Task<T> AddItemAsync<T>(T item) where T : EntityBase;

        Task<IEnumerable<T>> UpdateItemsAsync<T>(IEnumerable<T> items) where T : EntityBase;

        Task<T> UpdateItemAsync<T>(T document) where T : EntityBase;

        Task DeleteItemAsync<T>(T document) where T : EntityBase;
    }
}
