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
    [HubName(DashConstants.HubShapeName)]
    public class ShapeHub : Hub<IClientContractShapeHub>, IServerContractShapeHub
    {
        private readonly IDocumentRepository _documentRepository;

        public ShapeHub(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        public async Task<Result> UpdateShapePosition(string id, double x, double y)
        {
            var shapeModel = (await _documentRepository.GetItemsAsync<ShapeModel>(item => item.Id == id)).FirstOrDefault();

            if (shapeModel != null)
            {
                Clients.Others.MoveShapeTo(id, x, y);
            }

            var result = new Result(true);
            return result;

        }

        public async Task<Result> UpdateShapeSize(string id, double width, double height)
        {
            var shapeModel = (await _documentRepository.GetItemsAsync<ShapeModel>(item => item.Id == id)).FirstOrDefault();

            if (shapeModel != null)
            {
                Clients.Others.SetShapeSizeTo(id, width, height);
            }

            var result = new Result(true);
            return result;
        }
    }
}