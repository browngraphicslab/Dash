using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class FieldEndpoint
    {
        private readonly ServerEndpoint _connection;
        private static int count;

        public FieldEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
        }

        /// <summary>
        ///     Adds a fieldModel to the server
        /// </summary>
        /// <param name="newField"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void AddField(FieldModel newField, Action<FieldModelDTO> success, Action<Exception> error)
        {
            Debug.WriteLine(count++);
            Task.Run(() =>
            {
                try
                {
                    // convert from field model to DTO
                    var dto = newField.GetFieldDTO();
                    var result = _connection.Post("api/Field", dto);
                    var resultDto = result.Content.ReadAsAsync<FieldModelDTO>().Result;

                    success(resultDto);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }

        /// <summary>
        ///     Updates a field model on the server.
        /// </summary>
        /// <param name="fieldToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void UpdateField(FieldModel fieldToUpdate, Action<FieldModelDTO> success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                try
                {
                    var dto = fieldToUpdate.GetFieldDTO();
                    var result = _connection.Put("api/Field", dto);
                    var resultDto = result.Content.ReadAsAsync<FieldModelDTO>().Result;

                    success(resultDto);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }

        public void GetField(string id, Action<FieldModelDTO> success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                try
                {
                    var fieldModelDTO = _connection.GetItem<FieldModelDTO>($"api/Field/{id}").Result;
                    success(fieldModelDTO);
                }
                catch (Exception e)
                {
                    // return the error message
                    error(e);
                }
            });
        }

        public void DeleteField(FieldModel fieldToDelete, Action success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                var id = fieldToDelete.Id;
                Debug.WriteLine(id);
                try
                {
                    _connection.Delete($"api/Field/{id}");
                    success();
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