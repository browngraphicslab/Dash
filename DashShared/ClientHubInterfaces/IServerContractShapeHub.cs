using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public interface IServerContractShapeHub
    {
        Task UpdateShapePosition(string id, double x, double y);

        Task UpdateShapeSize(string id, double width, double height);

    }
}
