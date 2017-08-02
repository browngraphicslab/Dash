using System.Net.Http;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ShapeEndpoint
    {
        private ServerEndpoint _connection;

        public ShapeEndpoint(ServerEndpoint connection)
        {
            _connection = connection;
        }

        public async Task<Result<ShapeModel>> CreateNewShape(ShapeModel newShape)
        {
            try
            {
                var result = _connection.Post("api/Shape", newShape);
                var shapeModel = await result.Content.ReadAsAsync<ShapeModel>();
                return new Result<ShapeModel>(true, shapeModel);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<ShapeModel>(false, string.Join("\n", e.Errors));
            }
        }

        public async Task<Result<ShapeModel>> UpdateCurrentShape(ShapeModel shapeToUpdate)
        {
            try
            {
                var result = _connection.Put("api/Shape", shapeToUpdate);
                var shapeModel = await result.Content.ReadAsAsync<ShapeModel>();
                return new Result<ShapeModel>(true, shapeModel);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<ShapeModel>(false, string.Join("\n", e.Errors));
            }
        }

        public async Task<Result<ShapeModel>> GetShape(string id)
        {
            try
            {
                var shapeModel = await _connection.GetItem<ShapeModel>($"api/Shape/{id}");
                return new Result<ShapeModel>(true, shapeModel);
            }
            catch (ApiException e)
            {
                // return the error message
                return new Result<ShapeModel>(false, string.Join("\n", e.Errors));
            }
        }

        public Result DeleteShape(string id)
        {
            try
            {
                _connection.Delete($"api/Shape/{id}");
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
