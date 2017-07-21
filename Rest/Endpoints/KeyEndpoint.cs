using System.Collections.Generic;
using DashShared;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;

namespace Dash
{
    public class KeyEndpoint
    {
        #region RemovedFakeLocal
        private Dictionary<string, Key> _keys;

        private int _numKeys;
        #endregion


        private ServerEndpoint _connection;

        public KeyEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
            _keys = new Dictionary<string, Key>();
        }

        public Key GetKeyAsync(string keyId)
        {
            return _keys.ContainsKey(keyId) ? _keys[keyId] : null;
        }

        public void DeleteKeyAsync(Key model)
        {
            _keys.Remove(model.Id);
        }

        public Key UpdateKeyAsync(Key model)
        {
            _keys[model.Id] = model;
            return model;
        }

        public Key CreateKeyAsync(string keyId, string name="")
        {

            var newKey = new Key
            {
                Id = keyId,
                Name = name
            };

            _keys[keyId] = newKey;

            return newKey;
        }

        // Server methods

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

                // convert from server dto back to Key model controller
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
                Debug.WriteLine(id);
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
            Debug.WriteLine(id);
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
