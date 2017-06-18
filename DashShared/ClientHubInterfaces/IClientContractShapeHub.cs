using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public interface IClientContractShapeHub
    {
        void MoveShapeTo(string id, double x, double y);

        void SetShapeSizeTo(string id, double width, double height);
    }
}
