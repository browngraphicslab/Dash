using System.Collections.Generic;
using DashShared;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;

namespace Dash
{
    public class KeyEndpoint
    {
        private ServerEndpoint _connection;

        public KeyEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Adds a new Key to the DashWebServer.
        /// </summary>
        /// <param name="newKey"></param>
        /// <returns></returns>
        public async Task<Result<Key>> AddKey(Key newKey)
        {
            try
            {
                HttpResponseMessage result = _connection.Post("api/Key", newKey);
                Key resultk = await result.Content.ReadAsAsync<Key>();

                // convert from server dto back to Key model controller
                return new Result<Key>(true, resultk);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<Key>(false, string.Join("\n", e.Errors));
            }
        }

        /// <summary>
        /// Updates an existing Key in the DashWebServer
        /// </summary>
        /// <param name="KeyToUpdate"></param>
        /// <returns></returns>
        public async Task<Result<Key>> UpdateKey(Key KeyToUpdate)
        {
            try
            {
                HttpResponseMessage result = _connection.Put("api/Key", KeyToUpdate);
                Key resultk = await result.Content.ReadAsAsync<Key>();

                return new Result<Key>(true, resultk);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<Key>(false, string.Join("\n", e.Errors));
            }
        }

        public async Task<Result<Key>> GetKey(string id)
        {
            try
            {
                Key Key = await _connection.GetItem<Key>($"api/Key/{id}");
                return new Result<Key>(true, Key);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<Key>(false, string.Join("\n", e.Errors));
            }
        }

        public Result DeleteKey(Key KeyToDelete)
        {
            string id = KeyToDelete.Id;
            try
            {
                _connection.Delete($"api/Key/{id}");
                return new Result(true);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result(false, string.Join("\n", e.Errors));
            }
        }
    }
}
