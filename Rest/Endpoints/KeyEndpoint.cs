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
        public async Task<Result<KeyController>> AddKey(KeyController newKey)
        {
            try
            {
                HttpResponseMessage result = _connection.Post("api/Key", newKey);
                KeyController resultk = await result.Content.ReadAsAsync<KeyController>();

                // convert from server dto back to Key model controller
                return new Result<KeyController>(true, resultk);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<KeyController>(false, string.Join("\n", e.Errors));
            }
        }

        /// <summary>
        /// Updates an existing Key in the DashWebServer
        /// </summary>
        /// <param name="KeyToUpdate"></param>
        /// <returns></returns>
        public async Task<Result<KeyController>> UpdateKey(KeyController KeyToUpdate)
        {
            try
            {
                HttpResponseMessage result = _connection.Put("api/Key", KeyToUpdate);
                KeyController resultk = await result.Content.ReadAsAsync<KeyController>();

                return new Result<KeyController>(true, resultk);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<KeyController>(false, string.Join("\n", e.Errors));
            }
        }

        public async Task<Result<KeyController>> GetKey(string id)
        {
            try
            {
                KeyController Key = await _connection.GetItem<KeyController>($"api/Key/{id}");
                return new Result<KeyController>(true, Key);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<KeyController>(false, string.Join("\n", e.Errors));
            }
        }

        public Result DeleteKey(KeyController KeyToDelete)
        {
            string id = KeyToDelete.GetId();
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
