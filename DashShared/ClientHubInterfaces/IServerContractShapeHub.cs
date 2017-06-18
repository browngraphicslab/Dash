using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public interface IServerContractShapeHub
    {
        Task<Result> UpdateShapePosition(string id, double x, double y);

        Task<Result> UpdateShapeSize(string id, double width, double height);

    }
}
