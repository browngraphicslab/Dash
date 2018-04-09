using System;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public static class ControllerExtensions
    {
        public static async void CreateOnServer<TModel>(this IController<TModel> controller, Func<IController<TModel>, Task> failureCallback) where TModel : EntityBase
        {
            var failed = false;
            if (failed)
            {
                await failureCallback(controller);
            }
        }
    }
}
