using System;
using System.Net.Http;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class KeyEndpoint
    {
        private readonly ServerEndpoint _connection;

        public KeyEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// </summary>
        /// <param name="newKey"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async void AddKey(KeyModel newKey, Action<KeyModel> success, Action<Exception> error)
        {
            return;
            try
            {
                var result = await _connection.Post("api/Key", newKey);
                var resultK = await result.Content.ReadAsAsync<KeyModel>();

                success(resultK);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }

        }

        /// <summary>
        /// </summary>
        /// <param name="keyToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async void UpdateKey(KeyModel keyToUpdate, Action<KeyModel> success, Action<Exception> error)
        {
            return;
            try
            {
                var result = await _connection.Put("api/Key", keyToUpdate);
                var resultK = await result.Content.ReadAsAsync<KeyModel>();

                success(resultK);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }

        public async void GetKey(string id, Action<KeyModel> success, Action<Exception> error)
        {
            try
            {
                var key = await _connection.GetItem<KeyModel>($"api/Key/{id}");

                success(key);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }

        public async void DeleteKey(KeyModel keyToDelete, Action success, Action<Exception> error)
        {
            try
            {
                var response = await _connection.Delete($"api/Key/{keyToDelete.Id}");
                if (response.IsSuccessStatusCode)
                    success();
                else
                    error(new ApiException(response));
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }
    }
}