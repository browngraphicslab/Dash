using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dash.Rest.Endpoints;
using DashShared;

namespace Dash
{
    public class KeyEndpoint : Endpoint<KeyModel, KeyModel>, IKeyEndpoint
    {
        private readonly ServerEndpoint _connection;

        public KeyEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
            AddBatchHandler(AddKeys);
        }

        /// <summary>
        /// </summary>
        /// <param name="newKey"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void AddKey(KeyModel newKey, Action<KeyModel> success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                AddRequest(AddKeys, new Tuple<KeyModel, Action<KeyModel>, Action<Exception>>(newKey, success, error));
            });
        }

        private async void AddKeys(List<Tuple<KeyModel, Action<KeyModel>, Action<Exception>>> batch)
        {
            try
            {
                // convert from field models to DTOs
                var keys = batch.Select(x => x.Item1).ToList();
                var result = await _connection.Post("api/Key/batch", keys);
                var resultKeys = await result.Content.ReadAsAsync<List<KeyModel>>();

                var successHandlers = batch.Select(x => x.Item2);
                successHandlers.Zip(resultKeys, (success, key) =>
                {
                    success(key);
                    return true;
                });
            }
            catch (Exception e)
            {
                // return the error message
                batch.ForEach(x => x.Item3(e));
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="keyToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public async void UpdateKey(KeyModel keyToUpdate, Action<KeyModel> success, Action<Exception> error)
        {
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