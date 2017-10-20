﻿using System.Net.WebSockets;
using System.Threading.Tasks;
using DashShared;

namespace DashWebServer
{
    public class PushHandler : WebSocketHandler
    {
        public PushHandler(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
        {
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);

            var socketId = WebSocketConnectionManager.GetId(socket);

            var message = new Message
            {
                MessageType = MessageType.Text,
                Data = $"{socketId} is now connected"
            };

            await SendMessageToAllAsync(message);
        }

        public async Task SendMessage(string socketId, string message)
        {
            await InvokeClientMethodToAllAsync("receiveMessage", socketId, message);
        }
        public void SendCreate(EntityBase model)
        {
            Send(model, PushType.Create);
        }

        public void SendUpdate(EntityBase model)
        {
            Send(model, PushType.Update);
        }
        public void SendDelete(string id)
        {
            Send(id, null, PushType.Delete);
        }

        private void Send(string id, EntityBase model, PushType action)
        {
            var message = new PushMessage
            {
                PushType = action,
                Id = id,
                Model = model
            };

            InvokeClientMethodToAllAsync("receiveMessage", message);
        }

        private void Send(EntityBase model, PushType action)
        {
            Send(model.Id, model, action);
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);

            await base.OnDisconnected(socket);

            var message = new Message
            {
                MessageType = MessageType.Text,
                Data = $"{socketId} disconnected"
            };
            await SendMessageToAllAsync(message);
        }
    }
}
