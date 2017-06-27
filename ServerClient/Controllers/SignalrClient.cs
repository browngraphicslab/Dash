using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNet.SignalR.Client;

namespace Dash.ServerClient
{
    public class SignalrClient : IClientContractBasicHub
    {

        public async Task Initialize()
        {
            var hubConnection = new HubConnection(DashConstants.ServerBaseUrl + DashConstants.SignalrBaseUrl, false);
            await hubConnection.Start();

            IHubProxy<IServerContractBasicHub, IClientContractBasicHub> hubProxyBasic = hubConnection.CreateHubProxy<IServerContractBasicHub, IClientContractBasicHub>("serverHub");

            hubProxyBasic.SubscribeOn<int>(hub => hub.SomeInformationWithParam, SomeInformationWithParam);

            await hubProxyBasic.CallAsync(hub => hub.DoSomethingWithParam(6));

            var result = await hubProxyBasic.CallAsync(hub => hub.DoSomethingWithParamAndResult(10));

        }

        public void SomeInformation()
        {
            throw new NotImplementedException();
        }

        public void SomeInformationWithParam(int id)
        {
            throw new NotImplementedException();
        }

        public void SomeInformationWithText(string text)
        {
            throw new NotImplementedException();
        }
    }
}
