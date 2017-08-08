using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using DashShared;

namespace Dash
{
    public class FieldEndpoint
    {
        private readonly ServerEndpoint _connection;

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
        public void AddField(FieldModel newField, Action<FieldModelController> success, Action<Exception> error)
        {
            try
            {
                // convert from field model to DTO
                var dto = newField.GetFieldDTO();
                var result = _connection.Post("api/Field", dto);
                var resultDto = result.Content.ReadAsAsync<FieldModelDTO>().Result;

                // convert from server dto back to field model controller
                var controller = TypeInfoHelper.CreateFieldModelController(resultDto);
                success(controller);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }

        /// <summary>
        ///     Updates a field model on the server.
        /// </summary>
        /// <param name="fieldToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void UpdateField(FieldModel fieldToUpdate, Action<FieldModelController> success, Action<Exception> error)
        {
            try
            {
                var dto = fieldToUpdate.GetFieldDTO();
                var result = _connection.Put("api/Field", dto);
                var resultDto = result.Content.ReadAsAsync<FieldModelDTO>().Result;

                // convert from server dto back to field model controller
                var controller = TypeInfoHelper.CreateFieldModelController(resultDto);
                success(controller);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }

        public void GetField(string id, Action<FieldModelController> success, Action<Exception> error)
        {
            try
            {
                var fieldModelDTO = _connection.GetItem<FieldModelDTO>($"api/Field/{id}").Result;
                var controller = TypeInfoHelper.CreateFieldModelController(fieldModelDTO);
                success(controller);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }

        public void DeleteField(FieldModel fieldToDelete, Action success, Action<Exception> error)
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
        }
    }
}