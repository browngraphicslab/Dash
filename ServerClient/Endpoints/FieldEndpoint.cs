using System.Net.Http;
using System.Threading.Tasks;
using DashShared;

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

        public Result DeleteField(string id)
        {
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
    }
}
