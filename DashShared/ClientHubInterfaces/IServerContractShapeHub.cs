using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    /// <summary>
    /// Methods that the server shape hub must implement
    /// </summary>
    public interface IServerContractShapeHub
    {
        Task UpdateShapePosition(string id, double x, double y);

        Task UpdateShapeSize(string id, double width, double height);

        void SendNewShape(ShapeModel model);

    }
}
