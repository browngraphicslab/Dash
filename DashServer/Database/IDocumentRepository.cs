using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace DashServer
{
    /// <summary>
    /// The base class for our document repository, defines an interface so that we can
    /// implement our document respository through dependency injection
    /// </summary>
    public interface IDocumentRepository
    {

        Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate);

        Task<IEnumerable<T>> AddItemsAsync<T>(IEnumerable<T> items);

        Task<T> AddItemAsync<T>(T item);

        Task<IEnumerable<T>> UpdateItemsAsync<T>(IEnumerable<T> items);

        Task<T> UpdateItemAsync<T>(T item);

        Task<Document> DeleteItemAsync(string id);
    }
}
