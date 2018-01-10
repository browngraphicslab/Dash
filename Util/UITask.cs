using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;

namespace Dash
{
    public static class UITask
    {
        private static CoreDispatcher dispatcher;
        public static async void Run(Action uiTask)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    uiTask();
                });
        }

        public static Task RunTask(Action uiTask)
        {
            if (dispatcher == null)
                dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            var tcs = new TaskCompletionSource<bool>();
            var ignore = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    uiTask();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}