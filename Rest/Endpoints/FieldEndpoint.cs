using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dash.Rest.Endpoints;
using DashShared;

namespace Dash
{
    public class FieldEndpoint : Endpoint<FieldModel, FieldModelDTO>, IFieldEndpoint
    {
        private static readonly object l = new object();
        private static int count;
        private readonly ServerEndpoint _connection;

        public FieldEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
            AddBatchHandler(AddFields);
        }

        /// <summary>
        ///     Adds a fieldModel to the server
        /// </summary>
        /// <param name="newField"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void AddField(FieldModel newField, Action<FieldModelDTO> success, Action<Exception> error)
        {
            Task.Run(() =>
            {
                AddRequest(AddFields, new Tuple<FieldModel, Action<FieldModelDTO>, Action<Exception>>(newField, success, error));
            });
        }

        private async void AddFields(List<Tuple<FieldModel, Action<FieldModelDTO>, Action<Exception>>> batch)
        {
            try
             {
                // convert from field models to DTOs
                var dtos = batch.Select(x => x.Item1.GetFieldDTO()).ToList();
                var result = await _connection.Post("api/Field/batch", dtos);
                var resultDtos = await result.Content.ReadAsAsync<List<FieldModelDTO>>();

                var successHandlers = batch.Select(x => x.Item2);
                successHandlers.Zip(resultDtos, (success, dto) =>
                {
                    success(dto);
                    return true;
                });
                 lock (l)
                 {
                     Debug.WriteLine(count++);
                 }
            }
            catch (Exception e)
            {
                // return the error message
                batch.ForEach(x => x.Item3(e));
            }
        }

        /// <summary>
        ///     Updates a field model on the server.
        /// </summary>
        /// <param name="fieldToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public void UpdateField(FieldModel fieldToUpdate, Action<FieldModelDTO> success, Action<Exception> error)
        {
            Task.Run(async () =>
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
            });
        }

        public async Task GetField(string id, Action<FieldModelDTO> success, Action<Exception> error)
        {
            await Task.Run(async () =>
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
            });
        }

        public async Task DeleteField(FieldModel fieldToDelete, Action success, Action<Exception> error)
        {
            await Task.Run(async () =>
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
            });
        }
    }
}