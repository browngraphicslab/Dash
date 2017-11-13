using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Dash
{
    public static class UITask
    {
        public static async void Run(Action uiTask)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    uiTask();
                });
        }
    }
}