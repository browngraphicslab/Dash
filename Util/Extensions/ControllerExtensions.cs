using System;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public static class ControllerExtensions
    {
        public static async void CreateOnServer<TModel>(this Controller<TModel> controller, Func<Controller<TModel>, Task> failureCallback) where TModel : EntityBase
        {
            var failed = false;
            if (failed)
            {
                await failureCallback(controller);
            }
        }
    }
}
