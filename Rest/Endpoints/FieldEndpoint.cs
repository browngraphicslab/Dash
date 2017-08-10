using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
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
        public async Task AddField(FieldModel newField, Action<FieldModelDTO> success, Action<Exception> error)
        {
            try
            {
                // convert from field model to DTO

                var dto = newField.GetFieldDTO();
                var result = await _connection.Post("api/Field", dto);

                var resultDto = await result.Content.ReadAsAsync<FieldModelDTO>();

                success(resultDto);

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
        public async Task UpdateField(FieldModel fieldToUpdate, Action<FieldModelDTO> success, Action<Exception> error)
        {
            try
            {
                var dto = fieldToUpdate.GetFieldDTO();
                var result = await _connection.Put("api/Field", dto);
                var resultDto = await result.Content.ReadAsAsync<FieldModelDTO>();

                success(resultDto);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }
        }

        public async Task GetField(string id, Action<FieldModelDTO> success, Action<Exception> error)
        {
            try
            {
                var fieldModelDTO = await _connection.GetItem<FieldModelDTO>($"api/Field/{id}");
                success(fieldModelDTO);
            }
            catch (Exception e)
            {
                // return the error message
                error(e);
            }

        }

        public async Task DeleteField(FieldModel fieldToDelete, Action success, Action<Exception> error)
        {
            var id = fieldToDelete.Id;
            try
            {
                await _connection.Delete($"api/Field/{id}");
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