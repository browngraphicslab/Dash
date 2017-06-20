using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DashShared;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace DashServer.Hubs
{

    /// <summary>
    /// The hub is an object which the clients can call methods on, the clients call
    /// methods from the IServerContractShapeHub and they implement the methods in IClientContractShapeHub.
    /// So the Hub can in turn call methods on the clients.
    /// </summary>
    [HubName(DashConstants.HubShapeName)]
    public class ShapeHub : Hub<IClientContractShapeHub>, IServerContractShapeHub
    {
        private readonly IDocumentRepository _documentRepository;

        /// <summary>
        /// Create a new shape hub, currently this can only be done through the hubs internals
        /// but we should fine a way to do it outside of that
        /// </summary>
        /// <param name="documentRepository"></param>
        public ShapeHub(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// Called from the clients when they want to send an update to the shape's positions
        /// </summary>
        /// <returns></returns>
        public async Task UpdateShapePosition(string id, double x, double y)
        {
            // get the shape model which has to be updated
            var shapeModel = (await _documentRepository.GetItemsAsync<ShapeModel>(item => item.Id == id)).FirstOrDefault();

            if (shapeModel != null)
            {
                // update the x and y of the model
                shapeModel.X = x;
                shapeModel.Y = y;

                // update the clients except the caller
                Clients.Others.MoveShapeTo(id, x, y);

                // update the database
                await _documentRepository.UpdateItemAsync(shapeModel);
            }
        }

        /// <summary>
        /// Called from clients when they want to update the size of a shape
        /// </summary>
        /// <param name="id"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task UpdateShapeSize(string id, double width, double height)
        {
            var shapeModel = (await _documentRepository.GetItemsAsync<ShapeModel>(item => item.Id == id)).FirstOrDefault();

            if (shapeModel != null)
            {
                // update the x and y of the model
                shapeModel.Width = width;
                shapeModel.Height = height;

                // update the clients except the caller
                Clients.Others.SetShapeSizeTo(id, width, height);

                // update the database
                await _documentRepository.UpdateItemAsync(shapeModel);
            }
        }

        public void SendNewShape(ShapeModel model)
        {
            Clients.Others.AddShape(model);
        }
    }
}