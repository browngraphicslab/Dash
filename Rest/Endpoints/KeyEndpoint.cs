using System;
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
        /// 
        /// </summary>
        /// <param name="newKey"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void AddKey(KeyModel newKey, Action<KeyModel> success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                try
                {
                    var result = _connection.Post("api/Key", newKey);
                    var resultK = result.Content.ReadAsAsync<KeyModel>().Result;

                    success(resultK);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void UpdateKey(KeyModel keyToUpdate, Action<KeyModel> success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                try
                {
                    var result = _connection.Put("api/Key", keyToUpdate);
                    var resultK = result.Content.ReadAsAsync<KeyModel>().Result;

                    success(resultK);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }

        public void GetKey(string id, Action<KeyModel> success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                try
                {
                    var key = _connection.GetItem<KeyModel>($"api/Key/{id}").Result;

                    success(key);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }

        public void DeleteKey(KeyModel keyToDelete, Action success, Action<Exception> error)
        {
            string id = keyToDelete.Id;
            Task.Run(() =>
            {
                try
                {
                    var response = _connection.Delete($"api/Key/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        success();
                    }
                    else
                    {
                        error(new ApiException(response));
                    }
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }
    }
}
