using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    /// <summary>
    /// Methods that the client shapehub proxy must implement
    /// </summary>
    public interface IClientContractShapeHub
    {
        // all these methods must return void
        void AddShape(ShapeModel shapeModel);

        void MoveShapeTo(string id, double x, double y);

        void SetShapeSizeTo(string id, double width, double height);
    }
}
