using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ShapeViewModel : ViewModelBase
    {
        public ShapeModel ShapeModel { get; private set; }

        #region Bindings

        private double _width;

        public double Width
        {
            get { return _width; }
            set
            {
                if (SetProperty(ref _width, value))
                {
                     // update the datastore
                     // update local models
                };
            }
        }

        private double _height;

        public double Height
        {
            get { return _height; }
            set
            {
                if (SetProperty(ref _height, value))
                {
                    // update the datastore
                    // update local models
                };
            }
        }

        private double _x;

        public double X
        {
            get { return _x; }
            set
            {
                if (SetProperty(ref _x, value))
                {
                    // update the datastore
                    // update local models
                };
            }
        }

        private double _y;

        public double Y
        {
            get { return _y; }
            set
            {
                if (SetProperty(ref _y, value))
                {
                    // update the datastore
                    // update local models
                };
            }
        }

        #endregion

        public ShapeViewModel(ShapeModel shapeModel)
        {
            ShapeModel = Clone(shapeModel);
            Width = ShapeModel.Width;
            Height = ShapeModel.Height;
            X = ShapeModel.X;
            Y = ShapeModel.Y;
        }


        public void MoveShape(double translationX, double translationY)
        {
            X += translationX;
            Y += translationY;
        }
    }
}
