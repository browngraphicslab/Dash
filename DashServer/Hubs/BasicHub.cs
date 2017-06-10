using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DashShared;
using Microsoft.AspNet.SignalR;
using DashShared;

namespace DashServer.Hubs
{
    public class BasicHub : Hub<IClientContractBasicHub>, IServerContractBasicHub
    {

        public override Task OnConnected()
        {
            // Add your own code here.
            // For example: in a chat application, record the association between
            // the current connection ID and user name, and mark the user as online.
            // After the code in this method completes, the client is informed that
            // the connection is established; for example, in a JavaScript client,
            // the start().done callback is executed.
            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            // Add your own code here.
            // For example: in a chat application, you might have marked the
            // user as offline after a period of inactivity; in that case 
            // mark the user as online again.
            return base.OnReconnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            // Add your own code here.
            // For example: in a chat application, mark the user as offline, 
            // delete the association between the current connection id and user name.
            return base.OnDisconnected(stopCalled);
        }

        public void DoSomething()
        {
            // await the client calls or they are sent possibly simultaneously
            // awaiting just means the message has been completely sent, has not
            // knowledge of process on client side

            // send message to all clients
            Clients.All.SomeInformationWithParam(5);

            // send message to the calling client
            Clients.Caller.SomeInformation();

            // send message to clients except the caller
            Clients.Others.SomeInformation();
        }

        public void DoSomethingWithParam(int id)
        {
        }

        public async Task DoSomethingAsync()
        {
        }

        public int DoSomethingWithParamAndResult(int id)
        {
            return id + 1;
        }
    }


}