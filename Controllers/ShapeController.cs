using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class ShapeController
    {
        public delegate void OnShapePositionChanged(double newX, double newY);

        public delegate void OnShapeSizeChanged(double newWidth, double newHeight);

        public event OnShapePositionChanged ShapePositionChanged;

        public event OnShapeSizeChanged ShapeSizeChanged;

        public ShapeModel ShapeModel { get; private set; }

        public ShapeController(ShapeModel shapeModel)
        {
            ShapeModel = shapeModel;
        }

        public async Task SetShapeSize(double width, double height)
        {
            ShapeModel.Width = width;
            ShapeModel.Height = height;

            // update local
            ShapeSizeChanged?.Invoke(ShapeModel.Width, ShapeModel.Height);

            // update the server
            var result = await App.Instance.Container.GetRequiredService<ShapeProxy>().UpdateShapePosition(ShapeModel.Id, width, height);
        }

        public async Task SetShapePosition(double x, double y)
        {
            ShapeModel.X = x;
            ShapeModel.Y = y;

            // update local
            ShapePositionChanged?.Invoke(ShapeModel.X, ShapeModel.Y);

            // update the server
            var result = App.Instance.Container.GetRequiredService<ShapeProxy>().UpdateShapePosition(ShapeModel.Id, x, y);
        }
    }
}
