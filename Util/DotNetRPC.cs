using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Popups;

namespace Dash
{
    public static class DotNetRPC
    {
        private static bool Connected { get; set; } = false;

        public static async Task Init()
        {
            App.AppServiceConnected += AppOnAppServiceConnected;

            // launch the fulltrust process and for it to connect to the app service            
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
            else
            {
                MessageDialog dialog = new MessageDialog("This feature is only available on Windows 10 Desktop SKU");
                await dialog.ShowAsync();
            }
        }

        private static async void AppOnAppServiceConnected(object sender, EventArgs eventArgs)
        {
            Connected = true;
            // send the ValueSet to the fulltrust process
        }

        public static async Task<ValueSet> CallRPCAsync(ValueSet data)
        {
            if (!Connected)
            {
                Debug.WriteLine("Error: Can't send RPC before app is connected to the interop");
                return null;
            }
            AppServiceResponse response = await App.Connection.SendMessageAsync(data);

            // check the result
            response.Message.TryGetValue("RESPONSE", out var result);
            Debug.WriteLine(response.Message["DEBUG"] as string);
            if (result.ToString() != "SUCCESS")
            {
                return null;
            }

            return response.Message;

        }
    }
}
