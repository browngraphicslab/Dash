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
        private readonly ShapeController _shapeController;
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
                    // update local models
                    ShapeModel.Width = value;

                    // update the datastore
                    if (_updateDataStore)
                    {
                        _shapeController.SetShapeSize(Width, Height);
                    }
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
                    // update local models
                    ShapeModel.Height = value;

                    // update the datastore
                    if (_updateDataStore)
                    {
                        _shapeController.SetShapeSize(Width, Height);
                    }
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
                    // update local models
                    ShapeModel.X = value;

                    // update the datastore
                    if (_updateDataStore)
                    {
                        _shapeController.SetShapePosition(X, Y);
                    }
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
                    // update local models
                    ShapeModel.Y = value;

                    // update the datastore
                    if (_updateDataStore)
                    {
                        _shapeController.SetShapePosition(X, Y);
                    }
                };
            }
        }

        #endregion

        private bool _updateDataStore;

        public ShapeViewModel(ShapeController shapeController)
        {
            _shapeController = shapeController;
            ShapeModel = Clone(shapeController.ShapeModel);
            Width = ShapeModel.Width;
            Height = ShapeModel.Height;
            X = ShapeModel.X;
            Y = ShapeModel.Y;

            _updateDataStore = true;

            _shapeController.ShapePositionChanged += ShapeController_ShapePositionChanged;
            _shapeController.ShapeSizeChanged += ShapeController_ShapeSizeChanged;
        }

        private void ShapeController_ShapeSizeChanged(double newWidth, double newHeight)
        {
            _updateDataStore = false;
            Width = newWidth;
            Height = newHeight;
            _updateDataStore = true;
        }

        private void ShapeController_ShapePositionChanged(double newX, double newY)
        {
            _updateDataStore = false;
            X = newX;
            Y = newY;
            _updateDataStore = true;
        }

        public void MoveShape(double translationX, double translationY)
        {
            X += translationX;
            Y += translationY;
        }
    }
}
