using System.Net.Http;
using System.Threading.Tasks;
using DashShared;
using System.Diagnostics;
using System.Collections.Generic;

namespace Dash
{
    public class FieldEndpoint
    {
        private ServerEndpoint _connection;

        public FieldEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Adds a new field to the DashWebServer.
        /// </summary>
        /// <param name="newField"></param>
        /// <returns></returns>
        public async Task<Result<FieldModelController>> AddField(FieldModel newField)
        {
            try
            {
                Debug.WriteLine(newField.Id);
                // convert from field model to DTO
                FieldModelDTO dto = newField.GetFieldDTO();
                HttpResponseMessage result = _connection.Post("api/Field", dto);
                FieldModelDTO resultDto = await result.Content.ReadAsAsync<FieldModelDTO>();

                // convert from server dto back to field model controller
                FieldModelController controller = TypeInfoHelper.CreateFieldModelController(resultDto);
                return new Result<FieldModelController>(true, controller);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<FieldModelController>(false, string.Join("\n", e.Errors));
            }
        }

        /// <summary>
        /// Updates an existing field in the DashWebServer
        /// </summary>
        /// <param name="FieldToUpdate"></param>
        /// <returns></returns>
        public async Task<Result<FieldModelController>> UpdateField(FieldModel FieldToUpdate)
        {
            try
            {
                Debug.WriteLine(FieldToUpdate.Id);
                FieldModelDTO dto = FieldToUpdate.GetFieldDTO();
                HttpResponseMessage result = _connection.Put("api/Field", dto);
                FieldModelDTO resultDto = await result.Content.ReadAsAsync<FieldModelDTO>();

                // convert from server dto back to field model controller
                FieldModelController controller = TypeInfoHelper.CreateFieldModelController(resultDto);
                return new Result<FieldModelController>(true, controller);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<FieldModelController>(false, string.Join("\n", e.Errors));
            }
        }

        public async Task<Result<FieldModelController>> GetField(string id)
        {
            try
            {
                Debug.WriteLine(id);
                FieldModelDTO FieldModelDTO = await _connection.GetItem<FieldModelDTO>($"api/Field/{id}");
                FieldModelController fmc = TypeInfoHelper.CreateFieldModelController(FieldModelDTO);
                return new Result<FieldModelController>(true, fmc);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<FieldModelController>(false, string.Join("\n", e.Errors));
            }
        }

        public Result DeleteField(FieldModel fieldToDelete)
        {
            string id = fieldToDelete.Id;
            Debug.WriteLine(id);
            try
            {
                _connection.Delete($"api/Field/{id}");
                return new Result(true);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result(false, string.Join("\n", e.Errors));
            }
        }

        public Result<IDictionary<KeyController, FieldModelController>>  GetFieldsDictionary(Dictionary<KeyController, string> fields)
        {
            var controllersMap = new Dictionary<KeyController, FieldModelController>();

            foreach(var kv in fields)
            {
                var controller = GetField(kv.Value).Result.Content;
                controllersMap[kv.Key] = controller;
            }

            return new Result<IDictionary<KeyController, FieldModelController>>(true, controllersMap);
        }
    }
}
