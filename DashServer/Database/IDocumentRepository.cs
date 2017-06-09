using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace DashServer
{
    public interface IDocumentRepository
    {

        Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate);

        Task AddItemsAsync<T>(IEnumerable<T> items);

        Task<Document> AddItemAsync<T>(T item);

        Task<Document> DeleteItemAsync(string id);
    }
}
