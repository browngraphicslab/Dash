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

        private static async void ConnectionOnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            ValueSet data = args.Request.Message;
            var type = data["REQUEST"] as string;
            switch (type)
            {
                case "Chrome":
                    string requestData = data["DATA"] as string;
                    Debug.WriteLine("Request data: " + requestData);
                    await BrowserView.HandleIncomingMessage(requestData);
                    break;
                default://Unhandled request
                    throw new NotImplementedException();
            }
            Debug.WriteLine("Received Request " + data["DEBUG"]);
        }

        private static void AppOnAppServiceConnected()
        {
            Connected = true;
            App.Connection.RequestReceived += ConnectionOnRequestReceived;
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
            if (result.ToString() != "SUCCESS")
            {
                return null;
            }

            return response.Message;

        }

        public static async Task ChromeRequest(string data)
        {
            await CallRPCAsync(new ValueSet
            {
                ["REQUEST"] = "Chrome",
                ["DATA"] = data
            });
        }
    }
}
