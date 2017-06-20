using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class ShapeProxy : IClientContractShapeHub, IServerContractShapeHub
    {
        private readonly IHubProxy<IServerContractShapeHub, IClientContractShapeHub> _shapeProxy;

        public ShapeProxy(HubConnection hubConnection)
        {
            _shapeProxy = hubConnection.CreateHubProxy<IServerContractShapeHub, IClientContractShapeHub>(
                DashConstants.HubShapeName);

            _shapeProxy.SubscribeOn<string, double, double>(hub => hub.MoveShapeTo, MoveShapeTo);
            _shapeProxy.SubscribeOn<string, double, double>(hub => hub.SetShapeSizeTo, SetShapeSizeTo);
        }

        #region ClientMethods

        public void MoveShapeTo(string id, double x, double y)
        {
            var shapeController = ContentController.GetShapeController(id);
            shapeController.SetShapePosition(x, y);
        }

        public void SetShapeSizeTo(string id, double width, double height)
        {
            var shapeController = ContentController.GetShapeController(id);
            shapeController.SetShapeSize(width, height);
        }

        public void AddShape(ShapeModel shapeModel)
        {
            var newShapeController = new ShapeController(shapeModel);
            ContentController.AddShapeController(newShapeController);
        }


        #endregion

        #region ServerMethods

        public Task UpdateShapePosition(string id, double x, double y)
        {
            return _shapeProxy.CallAsync(hub => hub.UpdateShapePosition(id, x, y));
        }

        public Task UpdateShapeSize(string id, double width, double height)
        {
            return _shapeProxy.CallAsync(hub => hub.UpdateShapeSize(id, width, height));
        }

        public void SendNewShape(ShapeModel model)
        {
            Debug.Assert(false, "you should add new shapes by using the REST API");
            //_shapeProxy.CallAsync(hub => hub.SendNewShape(model));
        }

        #endregion
    }
}
