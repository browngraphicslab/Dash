using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public static class ModelExtensions
    {
        public static async Task<FieldControllerBase> NewController(this FieldModel model)
        {
            var controller = await FieldControllerFactory.CreateFromModel(model);
            return controller;
        }
    }
}
