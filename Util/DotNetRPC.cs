using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

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
                case "SizeChrome":
                    string sizeData = data["DATA"] as string;
                    var split = sizeData.Split(",");
                    if (split.Length == 2)
                    {
                        var w = int.Parse(split[0]);
                        var h = int.Parse(split[1]);
                        await MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                if (CollapseRequest.LastFrame != null)
                                {
                                    var gsplit = CollapseRequest.LastFrame.GetFirstAncestorOfType<SplitManager>();
                                    if (gsplit != null)
                                    {
                                        var scol     = Grid.GetColumn(gsplit);
                                        var maingrid = gsplit.GetFirstAncestorOfType<Grid>();
                                        maingrid.ColumnDefinitions[scol].Width = new Windows.UI.Xaml.GridLength(w);
                                    }
                                }
                            });
                    }
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
            object result = "";
            response.Message?.TryGetValue("RESPONSE", out result);
            if (result?.ToString() != "SUCCESS")
            {
                return null;
            }

            return response.Message;

        }

        public static Task ChromeRequest(string data)
        {
            return CallRPCAsync(new ValueSet
            {
                ["REQUEST"] = "Chrome",
                ["DATA"] = data
            });
        }

        public static Task OpenUri(Uri target)
        {
            return CallRPCAsync(new ValueSet
            {
                ["REQUEST"] = "OpenUri",
                ["DATA"] = target.OriginalString
            });
        }
    }
}
