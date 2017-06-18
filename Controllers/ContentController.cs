using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public static class ContentController
    {
        private static ConcurrentDictionary<string, ShapeController> _shapeControllers = new ConcurrentDictionary<string, ShapeController>();

        public static void AddShapeController(ShapeController controller)
        {
            _shapeControllers[controller.ShapeModel.Id] = controller;
        }

        public static ShapeController GetShapeController(string shapeId)
        {
            return _shapeControllers[shapeId];
        }

    }
}
